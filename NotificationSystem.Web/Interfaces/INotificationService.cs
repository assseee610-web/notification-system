using NotificationSystem.Shared.DTOs;

namespace NotificationSystem.Web.Interfaces;

public interface INotificationService
{
    Task<NotificationResponse> CreateNotificationAsync(CreateNotificationRequest request, Guid userId);
    Task<NotificationStatusResponse> GetStatusAsync(Guid notificationId);
    Task<PagedResponse<NotificationResponse>> GetHistoryAsync(NotificationFilterRequest filter);
}