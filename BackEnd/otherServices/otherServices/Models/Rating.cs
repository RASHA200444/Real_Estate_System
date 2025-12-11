using System.ComponentModel.DataAnnotations;

namespace otherServices.Models
{
    public class Rating
    {
        public long RatingId { get; set; }

        public long LandlordId { get; set; }
        public Landlord Landlord { get; set; }
        public long RaterId { get; set; }
        public User RaterUser { get; set; }

        [Range(1, 5)]
        public decimal Score { get; set; }

        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
