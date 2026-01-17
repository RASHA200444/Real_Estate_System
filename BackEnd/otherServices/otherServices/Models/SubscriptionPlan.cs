namespace otherServices.Models
{
    public class SubscriptionPlan
    {
        public long SubscriptionPlanId { get; set; }

        public string Name { get; set; } = string.Empty; // Monthly, Annual...

        public string Description { get; set; } = string.Empty;

        public int DurationInMonths { get; set; } // 1, 3, 6, 12

        public decimal Price { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ICollection<UserSubscription> UserSubscriptions { get; set; } = new List<UserSubscription>();

    }
}
