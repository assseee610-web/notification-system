using Microsoft.EntityFrameworkCore;
using NotificationSystem.Shared.Data;
using NotificationSystem.Shared.Models;

namespace NotificationSystem.Web.Services;

public class ArchivingService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;

    public ArchivingService(IServiceProvider serviceProvider, IConfiguration configuration)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var intervalHours = int.Parse(_configuration["Archiving:CleanupIntervalHours"] ?? "24");
        var retentionDays = int.Parse(_configuration["Archiving:RetentionDays"] ?? "30");

        Console.WriteLine($"[Archiving] Запущен. Интервал: {intervalHours} ч., хранение: {retentionDays} дн.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ArchiveOldNotifications(retentionDays);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Archiving] Ошибка: {ex.Message}");
            }

            // Ждём указанный интервал
            await Task.Delay(TimeSpan.FromHours(intervalHours), stoppingToken);
        }
    }

    private async Task ArchiveOldNotifications(int retentionDays)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var cutoffDate = DateTime.UtcNow.AddDays(-retentionDays);

        // Находим уведомления старше срока хранения
        var oldNotifications = await dbContext.Notifications
            .Where(n => n.CreatedAt < cutoffDate)
            .Where(n => n.Status == "Delivered" || n.Status == "Failed")
            .ToListAsync();

        if (oldNotifications.Count == 0)
        {
            Console.WriteLine($"[Archiving] Нет уведомлений для архивации.");
            return;
        }

        Console.WriteLine($"[Archiving] Найдено {oldNotifications.Count} уведомлений для архивации.");

        foreach (var notification in oldNotifications)
        {
            var archived = new ArchivedNotification
            {
                Id = notification.Id,
                Type = notification.Type,
                Recipient = notification.Recipient,
                Subject = notification.Subject,
                Body = notification.Body,
                Status = notification.Status,
                RetryCount = notification.RetryCount,
                CreatedByUserId = notification.CreatedByUserId,
                CreatedAt = notification.CreatedAt,
                ArchivedAt = DateTime.UtcNow
            };

            dbContext.ArchivedNotifications.Add(archived);

            var history = await dbContext.NotificationHistories
                .Where(h => h.NotificationId == notification.Id)
                .ToListAsync();
            dbContext.NotificationHistories.RemoveRange(history);
            dbContext.Notifications.Remove(notification);
        }

        await dbContext.SaveChangesAsync();
        Console.WriteLine($"[Archiving] Архивация завершена. Перенесено: {oldNotifications.Count} записей.");
    }
}