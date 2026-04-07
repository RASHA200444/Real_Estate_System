using otherServices.Models.Enums;

namespace otherServices.Models
{
    public class Notification
    {
        public long NotificationId { get; set; }
        public long UserId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public NotificationType Type { get; set; }
        public string? TargetUrl { get; set; } // لينك يوديه للصفحة المطلوبة
        public bool ReadStatus { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public User User { get; set; }
    }
}