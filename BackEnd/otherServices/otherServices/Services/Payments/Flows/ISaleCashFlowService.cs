using otherServices.Models.DTOs.Payments;

namespace otherServices.Services.Payments.Flows
{
    public interface ISaleCashFlowService
    {
        Task<object> ExecuteAsync(long userId, SaleCashRequestDto dto);
    }
}
