using System.ComponentModel.DataAnnotations;

namespace otherServices.Models.DTOs.TwoFactor
{
    public class TwoFactorVerifyDto
    {
        [Required]
        public string TwoFactorToken { get; set; } = null!;

        [Required]
        public string Code { get; set; } = null!;
    }
}
