using otherServices.Models.DTOs.Subscriptions;
using otherServices.Models;

namespace otherServices.Services.Interfaces.Admins
{
    public interface ISubscriptionPlanService
    {
        Task<SubscriptionPlanResponseDto> AddSubscriptionPlanAsync(AddSubscriptionPlanDto dto);
        Task<IEnumerable<SubscriptionPlanResponseDto>> GetAllSubscriptionPlansAsync();
        Task<SubscriptionPlanResponseDto?> GetSubscriptionPlanByIdAsync(long id);
        Task<SubscriptionPlanResponseDto?> UpdateSubscriptionPlanAsync(long id, UpdateSubscriptionPlanDto dto);

    }
}
