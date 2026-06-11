using Microsoft.EntityFrameworkCore;
using NotificationSystem.Shared.Data;
using NotificationSystem.Shared.Models;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

namespace NotificationSystem.Web.Services;

public class EmailProcessorService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private IConnection _connection;
    private IChannel _channel;

    public EmailProcessorService(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    public override async Task StartAsync(CancellationToken cancellationToken)
    {
        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:HostName"],
            UserName = _configuration["RabbitMQ:UserName"],
            Password = _configuration["RabbitMQ:Password"],
            Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672")
        };

        _connection = await factory.CreateConnectionAsync(cancellationToken);
        _channel = await _connection.CreateChannelAsync(cancellationToken: cancellationToken);

        Console.WriteLine("[EmailProcessor] Запущен. Ожидание сообщений из email_queue...");

        await base.StartAsync(cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var queueName = _configuration["RabbitMQ:EmailQueue"];

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (sender, args) =>
        {
            try
            {
                var body = args.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                Console.WriteLine($"[EmailProcessor] Получено сообщение: {message}");

                var notificationData = JsonSerializer.Deserialize<JsonElement>(message);
                var notificationId = notificationData.GetProperty("NotificationId").GetGuid();

                using (var scope = _serviceProvider.CreateScope())
                {
                    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                    var notification = await dbContext.Notifications
                        .FirstOrDefaultAsync(n => n.Id == notificationId, stoppingToken);

                    if (notification == null)
                    {
                        Console.WriteLine($"[EmailProcessor] Уведомление {notificationId} не найдено");
                        await _channel!.BasicAckAsync(args.DeliveryTag, false);
                        return;
                    }

                    notification.Status = "Processing";
                    notification.UpdatedAt = DateTime.UtcNow;
                    await dbContext.SaveChangesAsync(stoppingToken);

                    Console.WriteLine($"[EmailProcessor] Отправка Email на {notification.Recipient}:");
                    Console.WriteLine($"[EmailProcessor]   Тема: {notification.Subject}");
                    Console.WriteLine($"[EmailProcessor]   Текст: {notification.Body}");

                    var random = new Random();
                    var isSuccess = random.Next(100) > 30;

                    if (isSuccess)
                    {
                        notification.Status = "Delivered";
                        notification.UpdatedAt = DateTime.UtcNow;

                        var history = new NotificationHistory
                        {
                            NotificationId = notification.Id,
                            Status = "Sent",
                            AttemptedAt = DateTime.UtcNow
                        };
                        dbContext.NotificationHistories.Add(history);
                        Console.WriteLine($"[EmailProcessor] Email успешно отправлен!");
                    }
                    else
                    {
                        notification.RetryCount++;
                        notification.UpdatedAt = DateTime.UtcNow;
                        notification.Status = notification.RetryCount >= notification.MaxRetries ? "Failed" : "New";

                        var history = new NotificationHistory
                        {
                            NotificationId = notification.Id,
                            Status = "Failed",
                            ErrorMessage = "Ошибка отправки Email (имитация)",
                            AttemptedAt = DateTime.UtcNow
                        };
                        dbContext.NotificationHistories.Add(history);
                        Console.WriteLine($"[EmailProcessor] Ошибка (попытка {notification.RetryCount}/{notification.MaxRetries})");
                    }

                    await dbContext.SaveChangesAsync(stoppingToken);
                }

                await _channel!.BasicAckAsync(args.DeliveryTag, false);
                Console.WriteLine($"[EmailProcessor] Обработка завершена.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailProcessor] Ошибка: {ex.Message}");
                await _channel!.BasicNackAsync(args.DeliveryTag, false, true);
            }
        };

        await _channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer, cancellationToken: stoppingToken);
        Console.WriteLine($"[EmailProcessor] Подписан на очередь '{queueName}'");

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async void Dispose()
    {
        if (_channel != null)
            await _channel.CloseAsync();
        if (_connection != null)
            await _connection.CloseAsync();
        base.Dispose();
    }
}