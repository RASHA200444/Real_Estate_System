using System.ComponentModel.DataAnnotations;

namespace otherServices.Models.DTOs.Payments
{
    public class FinalizePaymentDto
    {
        [Required, MaxLength(100)]
        public string ExternalRef { get; set; } = null!;

        [Required, MinLength(3), MaxLength(4)]
        public string CVV { get; set; } = null!;
    }
}
