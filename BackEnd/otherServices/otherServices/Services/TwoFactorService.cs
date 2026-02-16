using OtpNet;

namespace otherServices.Services
{
    public class TwoFactorService : ITwoFactorService
    {
        public (string secretBase32, string otpAuthUri) GenerateSetup(string accountLabel)
        {
            var secretBytes = KeyGeneration.GenerateRandomKey(20);
            var secretBase32 = Base32Encoding.ToString(secretBytes);

            var issuer = "RentMate"; // غيرها لاسم سيستمك
            var label = $"{issuer}:{accountLabel}";

            var otpAuthUri =
                $"otpauth://totp/{Uri.EscapeDataString(label)}?secret={secretBase32}&issuer={Uri.EscapeDataString(issuer)}&digits=6&period=30";

            return (secretBase32, otpAuthUri);
        }

        public bool VerifyCode(string secretBase32, string code)
        {
            var secretBytes = Base32Encoding.ToBytes(secretBase32);
            var totp = new Totp(secretBytes, step: 30, totpSize: 6);

            // يسمح ±30 ثانية
            return totp.VerifyTotp(code, out _, new VerificationWindow(previous: 1, future: 1));
        }
    }
}
