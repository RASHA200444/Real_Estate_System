namespace otherServices.Models.DTOs.Ads
{
    public class CreateAdDto
    {
        public long PostId { get; set; }

        public string? Title { get; set; }
        public string? Body { get; set; }

        public int Priority { get; set; } = 0;
        public int MaxImpressionsPerUserPerDay { get; set; } = 1;

        public DateTime? StartAt { get; set; }
        public DateTime? EndAt { get; set; }
    }
}
