using Microsoft.AspNetCore.Mvc;
using otherServices.Services.Interfaces;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notificationService,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    // 🟢 Get All Notifications
    [HttpGet("{userId}")]
    public async Task<IActionResult> GetUserNotifications(long userId)
    {
        try
        {
            var result = await _notificationService.GetUserNotificationsAsync(userId);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching notifications for user {UserId}", userId);
            return StatusCode(500, "Something went wrong");
        }
    }

    // 🔵 Get Unread Count
    [HttpGet("{userId}/unread-count")]
    public async Task<IActionResult> GetUnreadCount(long userId)
    {
        try
        {
            var count = await _notificationService.GetUnreadCountAsync(userId);
            return Ok(count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting unread count for user {UserId}", userId);
            return StatusCode(500, "Something went wrong");
        }
    }

    // 🟡 Mark One As Read
    [HttpPut("{notificationId}/mark-as-read")]
    public async Task<IActionResult> MarkAsRead(long notificationId)
    {
        try
        {
            await _notificationService.MarkAsReadAsync(notificationId);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            _logger.LogWarning(ex, "Notification not found {Id}", notificationId);
            return NotFound(ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking notification {Id} as read", notificationId);
            return StatusCode(500, "Something went wrong");
        }
    }

    // 🔴 Mark All As Read
    [HttpPut("mark-all/{userId}")]
    public async Task<IActionResult> MarkAllAsRead(long userId)
    {
        try
        {
            await _notificationService.MarkAllAsReadAsync(userId);
            return NoContent();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking all notifications for user {UserId}", userId);
            return StatusCode(500, "Something went wrong");
        }
    }
}