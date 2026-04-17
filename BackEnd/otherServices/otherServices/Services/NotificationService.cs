using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Caching.Memory;
using RentMate.Hubs;
using otherServices.Models;
using otherServices.Models.Enums;
using otherServices.Models.DTOs;
using otherServices.Repositories;
using otherServices.Services.Interfaces;

namespace otherServices.Services
{
    public class NotificationService : INotificationService
    {
        private readonly IHubContext<NotificationHub> _hubContext;
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMemoryCache _cache;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            IHubContext<NotificationHub> hubContext,
            IUnitOfWork unitOfWork,
            IMemoryCache cache,
            ILogger<NotificationService> logger)
        {
            _hubContext = hubContext;
            _unitOfWork = unitOfWork;
            _cache = cache;
            _logger = logger;
        }

        private string GetCacheKey(long userId) => $"notifications_{userId}";
        private string GetUnreadCountKey(long userId) => $"notifications_count_{userId}";

        // 🟢 Get Notifications
        public async Task<List<NotificationDto>> GetUserNotificationsAsync(long userId)
        {
            var cacheKey = GetCacheKey(userId);

            try
            {
                if (_cache.TryGetValue(cacheKey, out List<NotificationDto> cached))
                {
                    _logger.LogInformation("Notifications from cache for user {UserId}", userId);
                    return cached;
                }

                var notifications = await _unitOfWork.Notifications
                    .FindAsync(n => n.UserId == userId);

                var result = notifications
                    .OrderByDescending(n => n.CreatedAt)
                    .Select(n => new NotificationDto
                    {
                        NotificationId = n.NotificationId,
                        Title = n.Title,
                        Content = n.Content,
                        Type = n.Type,
                        TargetUrl = n.TargetUrl,
                        ReadStatus = n.ReadStatus,
                        CreatedAt = n.CreatedAt
                    }).ToList();

                _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

                _logger.LogInformation("Notifications fetched from DB for user {UserId}", userId);

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching notifications for user {UserId}", userId);
                throw;
            }
        }

        // 🟡 Mark As Read
        public async Task MarkAsReadAsync(long notificationId)
        {
            try
            {
                var notification = await _unitOfWork.Notifications.GetByIdAsync(notificationId);

                if (notification == null)
                    throw new KeyNotFoundException("Notification not found");

                if (notification.ReadStatus)
                    return;

                notification.ReadStatus = true;

                await _unitOfWork.CompleteAsync();

                // invalidate cache
                _cache.Remove(GetCacheKey(notification.UserId));
                _cache.Remove(GetUnreadCountKey(notification.UserId));

                _logger.LogInformation("Notification {Id} marked as read", notificationId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking notification {Id} as read", notificationId);
                throw;
            }
        }

        // 🔴 Mark All
        public async Task MarkAllAsReadAsync(long userId)
        {
            try
            {
                var notifications = await _unitOfWork.Notifications
                    .FindAsync(n => n.UserId == userId && !n.ReadStatus);

                foreach (var n in notifications)
                    n.ReadStatus = true;

                await _unitOfWork.CompleteAsync();

                _cache.Remove(GetCacheKey(userId));
                _cache.Remove(GetUnreadCountKey(userId));

                _logger.LogInformation("All notifications marked as read for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error marking all notifications for user {UserId}", userId);
                throw;
            }
        }

        // 🔵 Unread Count
        public async Task<int> GetUnreadCountAsync(long userId)
        {
            var cacheKey = GetUnreadCountKey(userId);

            try
            {
                if (_cache.TryGetValue(cacheKey, out int cachedCount))
                {
                    return cachedCount;
                }

                var notifications = await _unitOfWork.Notifications
                    .FindAsync(n => n.UserId == userId && !n.ReadStatus);

                int count = notifications.Count();

                _cache.Set(cacheKey, count, TimeSpan.FromMinutes(5));

                return count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting unread count for user {UserId}", userId);
                throw;
            }
        }

        // 🟣 Send Notification
        public async Task SendNotificationAsync(long userId, string title, string content, NotificationType type, string? targetUrl = null)
        {
            try
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Title = title,
                    Content = content,
                    Type = type,
                    TargetUrl = targetUrl,
                    CreatedAt = DateTime.UtcNow
                };

                await _unitOfWork.Notifications.AddAsync(notification);
                await _unitOfWork.CompleteAsync();

                // invalidate cache
                _cache.Remove(GetCacheKey(userId));
                _cache.Remove(GetUnreadCountKey(userId));

                // SignalR
                await _hubContext.Clients.Group(userId.ToString())
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

                _logger.LogInformation("Notification sent to user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending notification to user {UserId}", userId);
                throw;
            }
        }
    }
}