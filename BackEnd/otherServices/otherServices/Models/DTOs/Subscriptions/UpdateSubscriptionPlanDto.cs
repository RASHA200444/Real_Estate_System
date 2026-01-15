using otherServices.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace otherServices.Models.DTOs.Subscriptions
{
    public class UpdateSubscriptionPlanDto
    {
        public string? Name { get; set; }

        public string? Description { get; set; }

        [EnumDataType(typeof(SubscriptionDuration))]
        public SubscriptionDuration? Duration { get; set; }

        [Range(0.01, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public decimal? Price { get; set; }

    }
}
