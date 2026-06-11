using NotificationSystem.Shared.DTOs;
using NotificationSystem.Shared.Models;

namespace NotificationSystem.Shared.Extensions;

public static class NotificationMapper
{
    public static NotificationResponse ToResponse(this Notification notification)
    {
        return new NotificationResponse
        {
            Id = notification.Id,
            Type = notification.Type,
            Recipient = notification.Recipient,
            Subject = notification.Subject,
            Body = notification.Body,
            Status = notification.Status,
            CreatedAt = notification.CreatedAt
        };
    }
}