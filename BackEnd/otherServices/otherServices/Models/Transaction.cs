using otherServices.Models.Enums;

namespace otherServices.Models
{
    public class Transaction
    {
        public long TransactionId { get; set; }

        public long UserId { get; set; }
        public User User { get; set; }

        public TransactionType Type { get; set; }
        // PurchaseProperty | Installment | Subscription

        public long? PostId { get; set; } // لو شراء شقة
        public Post? Post { get; set; }
        public UserSubscription? UserSubscription { get; set; }

        public decimal TotalAmount { get; set; }

        public Transaction_Status Status { get; set; }
        // Pending | Paid | Failed | Cancelled

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}