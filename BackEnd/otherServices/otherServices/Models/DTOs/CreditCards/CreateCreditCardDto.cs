using System;
using System.ComponentModel.DataAnnotations;
using otherServices.Models.Enums;

namespace otherServices.Models.DTOs.CreditCards
{
    public class CreateCreditCardDto
    {
        [Required]
        [MinLength(13)]
        [MaxLength(16)]
        [RegularExpression(@"^\d{13,16}$", ErrorMessage = "Card number must be 13–16 digits.")]
        public string CardNumber { get; set; } = string.Empty;

        [Required]
        [MaxLength(100)]
        public string CardHolderName { get; set; } = string.Empty;

        [Required]
        public DateTime ExpiryDate { get; set; }

        [Required]
        [RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVV must be 3 or 4 digits.")]
        public string CVV { get; set; } = string.Empty;

        //[Required]
        //public CardType CardType { get; set; }
    }
}
