using otherServices.Models.DTOs.Payments;

namespace otherServices.Services.Payments.Flows
{
    public interface ISubscribeProFlowService
    {
        Task<object> ExecuteAsync(long landlordUserId, SubscribeProRequestDto dto);
    }
}
