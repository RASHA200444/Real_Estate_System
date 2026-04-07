using Microsoft.AspNetCore.SignalR;
using RentMate.Hubs;
using otherServices.Models;
using otherServices.Models.Enums;
using otherServices.Repositories;
using otherServices.Services.Interfaces;

namespace RentMate.Services.Implementations
{
    public class NotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IUnitOfWork _unitOfWork;

        public NotificationService( IHubContext<NotificationHub> hubContext, IUnitOfWork unitOfWork)
        {
            _hubContext = hubContext;
            _unitOfWork = unitOfWork;
        }

        public async Task SendNotificationAsync(
            long userId,
            string title,
            string content,
            NotificationType type,
            string? targetUrl = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Content = content,
                Type = type,
                TargetUrl = targetUrl,
                ReadStatus = false,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.Notifications.AddAsync(notification);
            await _unitOfWork.CompleteAsync();

            await _hubContext
                .Clients
                .Group(userId.ToString())
                .SendAsync("ReceiveNotification", new
                {
                    notification.NotificationId,
                    notification.Title,
                    notification.Content,
                    notification.Type,
                    notification.TargetUrl,
                    notification.ReadStatus,
                    notification.CreatedAt
                });
        }
    }
}