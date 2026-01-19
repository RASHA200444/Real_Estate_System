using System.ComponentModel.DataAnnotations;

namespace otherServices.Models.DTOs.Payments
{
    public class SubscribeProRequestDto
    {
        [Required]
        public long SubscriptionPlanId { get; set; }

        [Required]
        public long PaymentCardId { get; set; }

        [Required]
        public string CVV { get; set; } = string.Empty;

        // زي ExternalRef بتاع الدفع عشان idempotency
        [Required]
        public string ExternalRef { get; set; } = string.Empty;
    }
}
