using otherServices.Models.DTOs.Payments;
using otherServices.Models.Enums;

namespace otherServices.Services.Payments
{
    public interface IMockGatewayService
    {
        void ValidateCard(string cardNumber, int expiryMonth, int expiryYear, string cvv);
        CardType DetectCardType(string cardNumber);

        // token generation exposed (عشان PaymentCardService يستخدمه)
        string GenerateToken(long userId, string cardNumber, int expiryMonth, int expiryYear);

        // helper للـ masking (اختياري)
        string MaskFromCardNumber(string cardNumber);
    }
}
