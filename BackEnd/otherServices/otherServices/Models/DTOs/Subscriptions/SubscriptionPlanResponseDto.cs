namespace otherServices.Models.DTOs.Subscriptions
{
    public class SubscriptionPlanResponseDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int DurationInMonths { get; set; }
        public decimal Price { get; set; }
    }
}
