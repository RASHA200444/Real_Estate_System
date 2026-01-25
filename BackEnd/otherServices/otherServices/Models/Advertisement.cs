namespace otherServices.Models
{
    public class Advertisement
    {
        public long AdvertisementId { get; set; }

        public long PostId { get; set; }
        public Post Post { get; set; }

        public long CreatedByAdminUserId { get; set; }
        public User CreatedByAdminUser { get; set; }

        public string? Title { get; set; }
        public string? Body { get; set; }

        public bool IsActive { get; set; } = true;

        public int Priority { get; set; } = 0; // الأعلى يظهر الأول
        public int MaxImpressionsPerUserPerDay { get; set; } = 1;

        public DateTime StartAt { get; set; } = DateTime.UtcNow;
        public DateTime? EndAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
