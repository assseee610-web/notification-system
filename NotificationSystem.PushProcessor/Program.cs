using Microsoft.Extensions.Configuration;
using Npgsql;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Text;
using System.Text.Json;

var configuration = new ConfigurationBuilder()
    .SetBasePath(AppContext.BaseDirectory)
    .AddJsonFile("appsettings.json")
    .Build();

Console.WriteLine("[PushProcessor] Запуск...");

var connectionString = configuration.GetConnectionString("DefaultConnection")!;

var factory = new ConnectionFactory
{
    HostName = configuration["RabbitMQ:HostName"],
    UserName = configuration["RabbitMQ:UserName"],
    Password = configuration["RabbitMQ:Password"],
    Port = int.Parse(configuration["RabbitMQ:Port"] ?? "5672")
};

using var connection = await factory.CreateConnectionAsync();
using var channel = await connection.CreateChannelAsync();
var queueName = configuration["RabbitMQ:PushQueue"];

await channel.QueueDeclareAsync(queueName, durable: true, exclusive: false, autoDelete: false);

var consumer = new AsyncEventingBasicConsumer(channel);
consumer.ReceivedAsync += async (sender, args) =>
{
    try
    {
        var body = args.Body.ToArray();
        var message = Encoding.UTF8.GetString(body);
        Console.WriteLine($"[PushProcessor] Получено: {message}");

        var data = JsonSerializer.Deserialize<JsonElement>(message);
        var notificationId = data.GetProperty("NotificationId").GetGuid();
        var recipient = data.GetProperty("Recipient").GetString();

        Console.WriteLine($"[PushProcessor] Имитация отправки Push на {recipient}...");
        await Task.Delay(500);

        var random = new Random();
        var isSuccess = random.Next(100) > 30;

        using var dbConnection = new NpgsqlConnection(connectionString);
        await dbConnection.OpenAsync();

        if (isSuccess)
        {
            using var updateCmd = new NpgsqlCommand(
                @"UPDATE ""Notifications"" SET ""Status"" = 'Delivered', ""UpdatedAt"" = @now WHERE ""Id"" = @id", dbConnection);
            updateCmd.Parameters.AddWithValue("now", DateTime.UtcNow);
            updateCmd.Parameters.AddWithValue("id", notificationId);
            await updateCmd.ExecuteNonQueryAsync();

            using var historyCmd = new NpgsqlCommand(
                @"INSERT INTO ""NotificationHistories"" (""Id"", ""NotificationId"", ""Status"", ""AttemptedAt"") VALUES (@hid, @nid, 'Sent', @now)", dbConnection);
            historyCmd.Parameters.AddWithValue("hid", Guid.NewGuid());
            historyCmd.Parameters.AddWithValue("nid", notificationId);
            historyCmd.Parameters.AddWithValue("now", DateTime.UtcNow);
            await historyCmd.ExecuteNonQueryAsync();

            Console.WriteLine($"[PushProcessor] Push отправлен успешно!");
        }
        else
        {
            using var checkCmd = new NpgsqlCommand(
                @"SELECT ""RetryCount"", ""MaxRetries"" FROM ""Notifications"" WHERE ""Id"" = @id", dbConnection);
            checkCmd.Parameters.AddWithValue("id", notificationId);
            var reader = await checkCmd.ExecuteReaderAsync();
            
            int retryCount = 0, maxRetries = 3;
            if (await reader.ReadAsync()) { retryCount = reader.GetInt32(0); maxRetries = reader.GetInt32(1); }
            reader.Close();

            retryCount++;
            var newStatus = retryCount >= maxRetries ? "Failed" : "New";

            using var updateCmd = new NpgsqlCommand(
                @"UPDATE ""Notifications"" SET ""Status"" = @status, ""RetryCount"" = @rc, ""UpdatedAt"" = @now WHERE ""Id"" = @id", dbConnection);
            updateCmd.Parameters.AddWithValue("status", newStatus);
            updateCmd.Parameters.AddWithValue("rc", retryCount);
            updateCmd.Parameters.AddWithValue("now", DateTime.UtcNow);
            updateCmd.Parameters.AddWithValue("id", notificationId);
            await updateCmd.ExecuteNonQueryAsync();

            using var historyCmd = new NpgsqlCommand(
                @"INSERT INTO ""NotificationHistories"" (""Id"", ""NotificationId"", ""Status"", ""ErrorMessage"", ""AttemptedAt"") VALUES (@hid, @nid, 'Failed', @err, @now)", dbConnection);
            historyCmd.Parameters.AddWithValue("hid", Guid.NewGuid());
            historyCmd.Parameters.AddWithValue("nid", notificationId);
            historyCmd.Parameters.AddWithValue("err", "Ошибка отправки Push (имитация)");
            historyCmd.Parameters.AddWithValue("now", DateTime.UtcNow);
            await historyCmd.ExecuteNonQueryAsync();

            Console.WriteLine($"[PushProcessor] Ошибка Push (попытка {retryCount}/{maxRetries})");
        }

        await channel.BasicAckAsync(args.DeliveryTag, false);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[PushProcessor] Ошибка: {ex.Message}");
        await channel.BasicNackAsync(args.DeliveryTag, false, true);
    }
};

await channel.BasicConsumeAsync(queue: queueName, autoAck: false, consumer: consumer);
Console.WriteLine($"[PushProcessor] Подписан на '{queueName}'. Ожидание...");
Console.WriteLine("Нажмите Ctrl+C для выхода.");

await Task.Delay(Timeout.Infinite);