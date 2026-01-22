using otherServices.Models.DTOs.Payments;

namespace otherServices.Services.Payments
{
    public interface IPaymentFlowService
    {
        Task<object> SaleCashAsync(long buyerUserId, SaleCashRequestDto dto);

        Task<object> SaleInstallmentAsync(long buyerUserId, SaleInstallmentRequestDto dto);

        Task<object> RentStartAsync(long landlordUserId, RentStartPaymentRequestDto dto);

        Task<object> PayRemainingAsync(PayRemainingRequestDto dto);

        Task<object> SubscribeProAsync(long landlordUserId, SubscribeProRequestDto dto);
    }
}
