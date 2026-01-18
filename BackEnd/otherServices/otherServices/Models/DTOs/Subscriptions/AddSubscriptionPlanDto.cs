using otherServices.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace otherServices.Models.DTOs.Subscriptions
{
    public class AddSubscriptionPlanDto
    {
        [Required]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required]
        [EnumDataType(typeof(SubscriptionDuration), ErrorMessage = "Duration must be one of: Monthly, Quarterly, SemiAnnual, Annual")]
        public SubscriptionDuration Duration { get; set; }

        [Required]
        [Range(1, double.MaxValue, ErrorMessage = "Price must be greater than zero.")]
        public decimal Price { get; set; }
    }
}
