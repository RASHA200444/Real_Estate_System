using System;
using System.ComponentModel.DataAnnotations;

namespace otherServices.Models.DTOs.CreditCards
{
    public class EditCreditCardDto
    {
        [MinLength(13)]
        [MaxLength(16)]
        [RegularExpression(@"^\d{13,16}$", ErrorMessage = "Card number must be 13–16 digits.")]
        public string? CardNumber { get; set; }

        [MaxLength(100)]
        public string? CardHolderName { get; set; }

        public DateTime? ExpiryDate { get; set; }

        [RegularExpression(@"^\d{3,4}$", ErrorMessage = "CVV must be 3 or 4 digits.")]
        public string? CVV { get; set; }

        // public CardType? CardType { get; set; }
    }
}
