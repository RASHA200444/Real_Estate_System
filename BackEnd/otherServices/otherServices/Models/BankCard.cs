using System.ComponentModel.DataAnnotations;
using otherServices.Models.Enums;

namespace otherServices.Models
{
    public class BankCard
    {
        public long BankCardId { get; set; }

        // محاكاة: بنك بيحتفظ بالرقم (إنت هتخزنه مشفّر)
        [Required, MaxLength(400)]
        public string CardNumberEncrypted { get; set; } = null!;

        // CVV مش بنخزنه نص صريح… نخزن Hash
        [Required, MaxLength(200)]
        public string CvvHash { get; set; } = null!;

        public CardType CardType { get; set; } = CardType.Unknown;

        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }

        public decimal Balance { get; set; } = 0m;

        public bool IsActive { get; set; } = true;

        // محاكاة "3 محاولات فشل" (زي ما قلتي)
        public int FailedChargeCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
