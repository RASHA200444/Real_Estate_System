using otherServices.Models.DTOs.Payments;

namespace otherServices.Services.Payments.Flows
{
    public interface IRentStartFlowService
    {
        Task<object> ExecuteAsync(long landlordUserId, RentStartPaymentRequestDto dto);
    }
}
