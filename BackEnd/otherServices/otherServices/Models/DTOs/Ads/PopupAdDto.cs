namespace otherServices.Models.DTOs.Ads
{
    public class PopupAdDto
    {
        public long AdId { get; set; }
        public long PostId { get; set; }

        public string? Title { get; set; }
        public string? Body { get; set; }

        // الباك يوجه
        public string NavigateTo { get; set; } = string.Empty;

        public DateTime? ExpiresAt { get; set; }
    }
}
