using otherServices.Models.DTOs.Payments;

namespace otherServices.Services.Payments.Flows
{
    public interface IPayRemainingFlowService
    {
        Task<object> ExecuteAsync(PayRemainingRequestDto dto);
    }
}
