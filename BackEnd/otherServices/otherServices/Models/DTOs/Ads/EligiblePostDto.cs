using otherServices.Models.Enums;

namespace otherServices.Models.DTOs.Ads
{
    public class EligiblePostDto
    {
        public long PostId { get; set; }
        public string? Title { get; set; }
        public double? Price { get; set; }
        public bool IsAuction { get; set; }

        public PropertyType Type { get; set; }
        public PropertyStatus Status { get; set; }
        public PostPendingStatus PendingStatus { get; set; }

        public long LandlordId { get; set; }
        public long LandlordUserId { get; set; }
        public string? LandlordName { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
