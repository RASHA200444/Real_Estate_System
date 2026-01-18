using otherServices.Models.Enums;

namespace otherServices.Models
{
    public class UserSubscription
    {
        public long UserSubscriptionId { get; set; }

        public long UserId { get; set; }
        public User User { get; set; }

        public long SubscriptionPlanId { get; set; }
        public SubscriptionPlan SubscriptionPlan { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public SubscriptionStatus Status { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public long? TransactionId { get; set; }
        public Transaction Transaction { get; set; }

    }
}
