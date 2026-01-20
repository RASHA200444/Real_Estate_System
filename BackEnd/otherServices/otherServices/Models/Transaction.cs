using otherServices.Models.Enums;

namespace otherServices.Models
{
    public class Transaction
    {
        public long TransactionId { get; set; }

        public long? PostId { get; set; }
        public Post Post { get; set; } = null!;

        public long UserId { get; set; }
        public User User { get; set; } = null!;

        // ✅ NEW: Link to winning/paid proposal (optional)
        public long? ProposalId { get; set; }
        public Proposal? Proposal { get; set; }

        public decimal Amount { get; set; }

        public string PaymentMethod { get; set; } = "Card";

        public TransactionStatus Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ✅ Idempotency
        public string ExternalRef { get; set; } = null!;

        public long? PaymentCardId { get; set; }

        public int Attempts { get; set; } = 0;
        public string? LastError { get; set; }

        // ✅ ربط اختياري بسطر دفع
        public long? PaymentScheduleId { get; set; }
        public PaymentSchedule? PaymentSchedule { get; set; }

        public TransactionState State { get; set; } = TransactionState.Pending;
        public TransactionKind Kind { get; set; }

        public long? ContractId { get; set; }
        public string? ContractHash { get; set; }

        public TransactionType Type { get; set; }

        public decimal FeeAmount { get; set; }
        public decimal NetToLandlord { get; set; }

        public UserSubscription? UserSubscription { get; set; }

        public long? LandlordUserId { get; set; }
        public long? AdminUserId { get; set; }
    }
}
