using otherServices.Models.DTOs.Payments;

namespace otherServices.Services.Payments
{
    public interface IPaymentCardService
    {
        Task<TokenizeCardResponseDto> TokenizeAndSaveAsync(long userId, TokenizeCardRequestDto dto);
        Task<IEnumerable<PaymentCardDto>> GetAllAsync(long userId);
        Task<PaymentCardDto> SetDefaultAsync(long userId, long paymentCardId);
        Task<bool> DeleteAsync(long userId, long paymentCardId);
        Task<bool> DeactivateAsync(long userId, long paymentCardId);

    }
}
