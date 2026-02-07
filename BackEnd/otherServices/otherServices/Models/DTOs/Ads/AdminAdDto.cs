namespace otherServices.Models.DTOs.Ads
{
    public class AdminAdDto
    {
        public long AdId { get; set; }
        public long PostId { get; set; }

        public string? Title { get; set; }
        public string? Body { get; set; }

        public bool IsActive { get; set; }

        public int Priority { get; set; }
        public int MaxImpressionsPerUserPerDay { get; set; }

        public DateTime StartAt { get; set; }
        public DateTime? EndAt { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
