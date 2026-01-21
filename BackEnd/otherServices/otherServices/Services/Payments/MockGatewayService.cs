using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using otherServices.Models.Enums;

namespace otherServices.Services.Payments
{
    public class MockGatewayService : IMockGatewayService
    {
        private readonly string _tokenSecret;

        public MockGatewayService(IConfiguration config)
        {
            _tokenSecret = config["MockGateway:TokenSecret"]
                ?? throw new Exception("Missing config: MockGateway:TokenSecret");
        }

        public void ValidateCard(string cardNumber, int expiryMonth, int expiryYear, string cvv)
        {
            var sanitized = Sanitize(cardNumber);

            if (string.IsNullOrWhiteSpace(sanitized))
                throw new Exception("CardNumber is required");

            if (!IsValidLuhn(sanitized))
                throw new Exception("Invalid card number");

            if (expiryMonth < 1 || expiryMonth > 12)
                throw new Exception("Invalid ExpiryMonth");

            if (expiryYear < 2024 || expiryYear > 2100)
                throw new Exception("Invalid ExpiryYear");

            if (IsExpired(expiryMonth, expiryYear))
                throw new Exception("Card expired");

            if (string.IsNullOrWhiteSpace(cvv) || cvv.Length < 3 || cvv.Length > 4)
                throw new Exception("Invalid CVV");
        }

        public CardType DetectCardType(string cardNumber)
        {
            var number = Sanitize(cardNumber);
            if (string.IsNullOrWhiteSpace(number)) return CardType.Unknown;

            if (number.StartsWith("4")) return CardType.Visa;
            if (number.StartsWith("5")) return CardType.MasterCard;
            if (number.StartsWith("34") || number.StartsWith("37")) return CardType.AmericanExpress;

            return CardType.Unknown;
        }

        public string GenerateToken(long userId, string cardNumber, int expiryMonth, int expiryYear)
        {
            var sanitized = Sanitize(cardNumber);
            var last4 = sanitized.Length >= 4 ? sanitized[^4..] : "0000";

            var raw = $"{userId}|{last4}|{expiryMonth:D2}/{expiryYear}|{Guid.NewGuid()}";

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_tokenSecret));
            var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(raw));

            return Convert.ToBase64String(hash);
        }

        public string MaskFromCardNumber(string cardNumber)
        {
            var sanitized = Sanitize(cardNumber);
            if (sanitized.Length < 4) return "****";
            return "****" + sanitized[^4..]; // ****1234  (طولها 8)
        }


        // ================= Helpers =================

        private string Sanitize(string input)
            => new string((input ?? "").Where(char.IsDigit).ToArray());

        private bool IsExpired(int month, int year)
        {
            var now = DateTime.UtcNow;
            if (year < now.Year) return true;
            if (year == now.Year && month < now.Month) return true;
            return false;
        }

        private bool IsValidLuhn(string cardNumber)
        {
            int sum = 0;
            bool alternate = false;

            for (int i = cardNumber.Length - 1; i >= 0; i--)
            {
                int n = cardNumber[i] - '0';
                if (alternate)
                {
                    n *= 2;
                    if (n > 9) n -= 9;
                }
                sum += n;
                alternate = !alternate;
            }
            return sum % 10 == 0;
        }
    }
}
