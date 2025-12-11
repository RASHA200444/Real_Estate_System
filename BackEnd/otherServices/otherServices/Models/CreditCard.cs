using otherServices.Models.Enums;
using System;
using System.ComponentModel.DataAnnotations;

namespace otherServices.Models
{
    public class CreditCard
    {
        public long CreditCardId { get; set; }

        public long UserId { get; set; }
        public User User { get; set; }

        [Required]
        [MaxLength(255)]
        public string CardNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string CardHolderName { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiryDate { get; set; } 

        [Required]
        [MaxLength(255)]
        public string CVV { get; set; } = string.Empty;

        public CardType CardType { get; set; }  // Visa = 0, MasterCard = 1, AmericanExpress = 2, Unknown = 3 
    }

}
