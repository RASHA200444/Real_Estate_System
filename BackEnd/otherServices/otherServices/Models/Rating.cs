using System.ComponentModel.DataAnnotations;

namespace otherServices.Models
{
    public class Rating
    {
        public long RatingId { get; set; }

        //  الشخص اللي بيتقيم (Owner فقط)
        public long LandlordId { get; set; }
        public Landlord Landlord { get; set; }

        // User الشخص اللي عمل التقييم أي 
        public long RaterId { get; set; }
        public User RaterUser { get; set; }

        //  قيمة التقييم
        [Range(1, 5)]
        public decimal Score { get; set; }

        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
