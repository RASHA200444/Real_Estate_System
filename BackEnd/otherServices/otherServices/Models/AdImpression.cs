namespace otherServices.Models
{
    public class AdImpression
    {
        public long AdImpressionId { get; set; }

        public long AdvertisementId { get; set; }
        public Advertisement Advertisement { get; set; }

        public long UserId { get; set; }
        public User User { get; set; }

        public DateTime SeenAt { get; set; } = DateTime.UtcNow;
        public DateTime? ClickedAt { get; set; }

        // yyyyMMdd to count per day
        public int DateKey { get; set; }
    }
}
