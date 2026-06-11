using Microsoft.EntityFrameworkCore;
using NotificationSystem.Shared.Data;
using NotificationSystem.Shared.DTOs;
using NotificationSystem.Shared.Extensions;
using NotificationSystem.Shared.Models;
using NotificationSystem.Web.Interfaces;

namespace NotificationSystem.Web.Services;

public class NotificationService : INotificationService
{
    private readonly AppDbContext _context;
    private readonly IRabbitMQService _rabbitMQService;
    private readonly ICacheService _cacheService;

    public NotificationService(AppDbContext context, IRabbitMQService rabbitMQService, ICacheService cacheService)
    {
        _context = context;
        _rabbitMQService = rabbitMQService;
        _cacheService = cacheService;
    }

    public async Task<NotificationResponse> CreateNotificationAsync(
        CreateNotificationRequest request, Guid userId)
    {
        var notification = new Notification
        {
            Type = request.Type,
            Recipient = request.Recipient,
            Subject = request.Subject,
            Body = request.Body,
            Status = "New",
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();

        var queueName = _rabbitMQService.GetQueueName(notification.Type);

        _rabbitMQService.PublishMessage(queueName, new
        {
            NotificationId = notification.Id,
            Type = notification.Type,
            Recipient = notification.Recipient,
            Subject = notification.Subject,
            Body = notification.Body
        });

        return notification.ToResponse();
    }

    public async Task<NotificationStatusResponse> GetStatusAsync(Guid notificationId)
    {
        // Проверяем кеш
        var cacheKey = $"notification_status_{notificationId}";
        var cached = await _cacheService.GetAsync<NotificationStatusResponse>(cacheKey);
        if (cached != null)
        {
            Console.WriteLine($"[Cache] Данные из кеша для {notificationId}");
            return cached;
        }

        var notification = await _context.Notifications
            .FirstOrDefaultAsync(n => n.Id == notificationId);

        if (notification == null)
            return null;

        var history = await _context.NotificationHistories
            .Where(h => h.NotificationId == notificationId)
            .OrderByDescending(h => h.AttemptedAt)
            .Select(h => new HistoryItem
            {
                Id = h.Id,
                Status = h.Status,
                ErrorMessage = h.ErrorMessage,
                AttemptedAt = h.AttemptedAt
            })
            .ToListAsync();

        var result = new NotificationStatusResponse
        {
            Id = notification.Id,
            Type = notification.Type,
            Recipient = notification.Recipient,
            Subject = notification.Subject,
            Body = notification.Body,
            Status = notification.Status,
            RetryCount = notification.RetryCount,
            CreatedAt = notification.CreatedAt,
            UpdatedAt = notification.UpdatedAt,
            History = history
        };

        // Сохраняем в кеш на 2 минуты
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(2));
        Console.WriteLine($"[Cache] Данные сохранены в кеш для {notificationId}");

        return result;
    }

    public async Task<PagedResponse<NotificationResponse>> GetHistoryAsync(NotificationFilterRequest filter)
    {
        // Ключ кеша на основе параметров фильтра
        var cacheKey = $"history_{filter.Type}_{filter.Status}_{filter.Recipient}_{filter.Page}_{filter.PageSize}";
        var cached = await _cacheService.GetAsync<PagedResponse<NotificationResponse>>(cacheKey);
        if (cached != null)
        {
            Console.WriteLine($"[Cache] История из кеша");
            return cached;
        }

        var query = _context.Notifications.AsQueryable();

        if (!string.IsNullOrWhiteSpace(filter.Recipient))
            query = query.Where(n => n.Recipient.Contains(filter.Recipient));

        if (!string.IsNullOrWhiteSpace(filter.Type))
            query = query.Where(n => n.Type == filter.Type);

        if (!string.IsNullOrWhiteSpace(filter.Status))
            query = query.Where(n => n.Status == filter.Status);

        if (filter.FromDate.HasValue)
            query = query.Where(n => n.CreatedAt >= filter.FromDate.Value);

        if (filter.ToDate.HasValue)
            query = query.Where(n => n.CreatedAt <= filter.ToDate.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(n => n.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(n => n.ToResponse())
            .ToListAsync();

        var result = new PagedResponse<NotificationResponse>
        {
            Items = items,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize
        };

        // Кешируем на 5 минут
        await _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
        Console.WriteLine($"[Cache] История сохранена в кеш");

        return result;
    }
}