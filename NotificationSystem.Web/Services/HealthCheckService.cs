using Npgsql;
using RabbitMQ.Client;
using StackExchange.Redis;

namespace NotificationSystem.Web.Services;

public class HealthCheckService
{
    private readonly IConfiguration _configuration;

    public HealthCheckService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task<Dictionary<string, object>> CheckAllAsync()
    {
        var results = new Dictionary<string, object>();

        // Проверка PostgreSQL
        try
        {
            using var connection = new NpgsqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await connection.OpenAsync();
            results["PostgreSQL"] = "Healthy";
        }
        catch (Exception ex)
        {
            results["PostgreSQL"] = $"Unhealthy: {ex.Message}";
        }

        // Проверка Redis
        try
        {
            var redis = ConnectionMultiplexer.Connect(_configuration["Redis:ConnectionString"]!);
            var db = redis.GetDatabase();
            await db.PingAsync();
            results["Redis"] = "Healthy";
            redis.Close();
        }
        catch (Exception ex)
        {
            results["Redis"] = $"Unhealthy: {ex.Message}";
        }

        // Проверка RabbitMQ
        try
        {
            var factory = new ConnectionFactory
            {
                HostName = _configuration["RabbitMQ:HostName"],
                UserName = _configuration["RabbitMQ:UserName"],
                Password = _configuration["RabbitMQ:Password"],
                Port = int.Parse(_configuration["RabbitMQ:Port"] ?? "5672")
            };
            using var connection = await factory.CreateConnectionAsync();
            results["RabbitMQ"] = "Healthy";
        }
        catch (Exception ex)
        {
            results["RabbitMQ"] = $"Unhealthy: {ex.Message}";
        }

        var allHealthy = results.Values.All(v => v.ToString() == "Healthy");
        results["OverallStatus"] = allHealthy ? "Healthy" : "Unhealthy";

        return results;
    }
}