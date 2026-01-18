using System.ComponentModel.DataAnnotations;

namespace otherServices.Models.DTOs.Payments
{
    public class PurchasePostRequestDto
    {
        [Required]
        public long PostId { get; set; }

        [Required]
        public long UserId { get; set; }

        [Required]
        public long PaymentCardId { get; set; }

        // ✅ CVV وقت الدفع فقط (مش بيتخزن)
        [Required]
        public string CVV { get; set; } = null!;

        // ✅ لازم من الفرونت
        [Required]
        public string IdempotencyKey { get; set; } = null!;
    }

    public class PurchasePostResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public long? TransactionId { get; set; }
    }
}
