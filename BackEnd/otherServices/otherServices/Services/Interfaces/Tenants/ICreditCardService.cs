using otherServices.Models.DTOs.CreditCards;
using otherServices.Models;

namespace otherServices.Services.Interfaces.Tenants
{
    public interface ICreditCardService
    {
        Task<CreditCardDto> AddCreditCardAsync(long userId, CreateCreditCardDto dto);
        Task<CreditCardDto> EditCreditCardAsync(long userId, long cardId, EditCreditCardDto dto);
        Task<IEnumerable<CreditCardDto>> GetAllCardsAsync(long userId);
        Task<CreditCardDto> GetCreditCardByIdAsync(long userId, long cardId);
        Task<bool> DeleteCreditCardAsync(long userId, long cardId);
    }
}
