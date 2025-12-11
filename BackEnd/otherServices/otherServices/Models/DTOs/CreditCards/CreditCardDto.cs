using System;
using otherServices.Models.Enums;
namespace otherServices.Models.DTOs.CreditCards
{
    public class CreditCardDto
    {
        public long CreditCardId { get; set; }
        public long UserId { get; set; }
        public string MaskedCardNumber { get; set; } = string.Empty;
        public string CardHolderName { get; set; } = string.Empty;
        public DateTime ExpiryDate { get; set; } 
        public CardType CardType { get; set; } 
    }
}
