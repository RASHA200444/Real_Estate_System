using otherServices.Models.DTOs;
using otherServices.Models.Enums;

namespace otherServices.Services.Interfaces
{
    public interface INotificationService
    {
        Task<List<NotificationDto>> GetUserNotificationsAsync(long userId);
        Task MarkAsReadAsync(long notificationId);
        Task MarkAllAsReadAsync(long userId);
        Task<int> GetUnreadCountAsync(long userId);
        Task SendNotificationAsync(long userId, string title, string content, NotificationType type, string? targetUrl = null);
    }
}