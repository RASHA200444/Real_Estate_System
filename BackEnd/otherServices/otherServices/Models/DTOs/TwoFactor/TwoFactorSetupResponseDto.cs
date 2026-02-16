namespace otherServices.Models.DTOs.TwoFactor
{
    public class TwoFactorSetupResponseDto
    {
        public bool AlreadyEnabled { get; set; }
        public string? SecretBase32 { get; set; }
        public string? OtpAuthUri { get; set; }
    }
}
