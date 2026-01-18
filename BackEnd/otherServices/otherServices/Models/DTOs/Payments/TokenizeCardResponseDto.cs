using otherServices.Models.Enums;

namespace otherServices.Models.DTOs.Payments
{
    public class TokenizeCardResponseDto
    {
        public long PaymentCardId { get; set; }
        public string MaskedCardNumber { get; set; } = null!;
        public CardType CardType { get; set; }
        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; }
        public bool IsDefault { get; set; }
    }
}
