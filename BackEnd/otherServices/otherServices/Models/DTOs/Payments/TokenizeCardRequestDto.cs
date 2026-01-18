using Microsoft.AspNetCore.Mvc;
using otherServices.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace otherServices.Models.DTOs.Payments
{
    public class TokenizeCardRequestDto
    {
        [FromForm, Required]
        public string CardNumber { get; set; } = null!; // حساس (مش بيتخزن)

        [FromForm, Required]
        public string CardHolderName { get; set; } = null!;

        [FromForm, Required, Range(1, 12)]
        public int ExpiryMonth { get; set; }

        [FromForm, Required, Range(2024, 2100)]
        public int ExpiryYear { get; set; }

        [FromForm, Required]
        public string CVV { get; set; } = null!; // حساس (مش بيتخزن)
    }
}
