namespace otherServices.Models
{
    public class Notification
    {
        public long NotificationId { get; set; }
        public long UserId { get; set; }
        public string Content { get; set; }
        public bool ReadStatus { get; set; }
        public DateTime CreatedAt { get; set; }

        public User User { get; set; }
    }
}
