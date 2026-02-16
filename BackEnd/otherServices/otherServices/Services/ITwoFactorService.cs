namespace otherServices.Services
{
    public interface ITwoFactorService
    {
        (string secretBase32, string otpAuthUri) GenerateSetup(string accountLabel);
        bool VerifyCode(string secretBase32, string code);
    }
}
