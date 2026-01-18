using otherServices.Models.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace otherServices.Models
{
    public class PaymentCard
    {
        public long PaymentCardId { get; set; }

        public long UserId { get; set; }
        public User User { get; set; } = null!;

        // ✅ Token مش رقم كارت + هنخزنه مشفر
        [Required]
        [MaxLength(255)]
        public string CardTokenEncrypted { get; set; } = string.Empty;

        // ✅ آخر 4 (غير حساس) أو Masked
        [Required]
        [MaxLength(30)]
        public string MaskedCardNumber { get; set; } = "****";

        public CardType CardType { get; set; } = CardType.Unknown;

        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }

        public bool IsDefault { get; set; } = false;
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
