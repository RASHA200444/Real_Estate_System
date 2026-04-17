using otherServices.Models.Enums;

namespace otherServices.Models.DTOs
{
    public class NotificationDto
    {
        public long NotificationId { get; set; }
        public string Title { get; set; }
        public string Content { get; set; }
        public NotificationType Type { get; set; }
        public string? TargetUrl { get; set; }
        public bool ReadStatus { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
