using otherServices.Models.Enums;

namespace otherServices.Services.Interfaces
{
    public interface INotificationService
    {
        Task SendNotificationAsync(
            long userId,
            string title,
            string content,
            NotificationType type,
            string? targetUrl = null
        );
    }
}