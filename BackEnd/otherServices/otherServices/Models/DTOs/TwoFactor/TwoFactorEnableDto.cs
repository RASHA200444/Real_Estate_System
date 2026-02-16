using System.ComponentModel.DataAnnotations;

namespace otherServices.Models.DTOs.TwoFactor
{
    public class TwoFactorEnableDto
    {
        [Required]
        public string Code { get; set; } = null!;
    }
}
