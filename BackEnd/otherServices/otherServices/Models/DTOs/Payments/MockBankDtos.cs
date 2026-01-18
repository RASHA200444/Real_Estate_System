using System.ComponentModel.DataAnnotations;

namespace otherServices.Models.DTOs.Payments
{
    public class SeedBankCardRequestDto
    {
        [Required] public string CardNumber { get; set; } = null!;
        [Required] public string CVV { get; set; } = null!;
        [Range(1, 12)] public int ExpiryMonth { get; set; }
        [Range(2024, 2100)] public int ExpiryYear { get; set; }
        [Range(0, 999999999)] public decimal Balance { get; set; }
    }

    public class ChargeCardRequestDto
    {
        [Required] public string CardToken { get; set; } = null!;
        [Required] public string CVV { get; set; } = null!;
        [Range(1, 999999999)] public decimal Amount { get; set; }
    }

    public class ChargeCardResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public decimal? RemainingBalance { get; set; }
    }
}
