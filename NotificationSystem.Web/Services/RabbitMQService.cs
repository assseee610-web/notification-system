using RabbitMQ.Client;
using NotificationSystem.Web.Interfaces;
using System.Text;
using System.Text.Json;

namespace NotificationSystem.Web.Services;

public class RabbitMQService : IRabbitMQService, IDisposable
{
    private readonly IConnection _connection;
    private readonly IChannel _channel;
    private readonly IConfiguration _configuration;

    public RabbitMQService(IConfiguration configuration)
    {
        _configuration = configuration;

        var factory = new ConnectionFactory
        {
            HostName = _configuration["RabbitMQ:HostName"],
            UserName = _configuration["RabbitMQ:UserName"],
            Password = _configuration["RabbitMQ:Password"],
            Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672")
        };

        _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
        _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

        _channel.QueueDeclareAsync(_configuration["RabbitMQ:EmailQueue"], durable: true, exclusive: false, autoDelete: false).GetAwaiter().GetResult();
        _channel.QueueDeclareAsync(_configuration["RabbitMQ:SmsQueue"], durable: true, exclusive: false, autoDelete: false).GetAwaiter().GetResult();
        _channel.QueueDeclareAsync(_configuration["RabbitMQ:PushQueue"], durable: true, exclusive: false, autoDelete: false).GetAwaiter().GetResult();

        Console.WriteLine("[RabbitMQ] Подключено. Очереди созданы.");
    }

    public void PublishMessage(string queueName, object message)
    {
        var json = JsonSerializer.Serialize(message);
        var body = Encoding.UTF8.GetBytes(json);

        var properties = new BasicProperties();
        properties.Persistent = true;

        _channel.BasicPublishAsync(exchange: "", routingKey: queueName, mandatory: true, basicProperties: properties, body: body).GetAwaiter().GetResult();

        Console.WriteLine($"[RabbitMQ] Отправлено в '{queueName}': {json}");
    }

    public string GetQueueName(string notificationType)
    {
        return notificationType.ToLower() switch
        {
            "email" => _configuration["RabbitMQ:EmailQueue"]!,
            "sms" => _configuration["RabbitMQ:SmsQueue"]!,
            "push" => _configuration["RabbitMQ:PushQueue"]!,
            _ => throw new ArgumentException($"Неизвестный тип: {notificationType}")
        };
    }

    public void Dispose()
    {
        _channel?.CloseAsync().GetAwaiter().GetResult();
        _connection?.CloseAsync().GetAwaiter().GetResult();
    }
}