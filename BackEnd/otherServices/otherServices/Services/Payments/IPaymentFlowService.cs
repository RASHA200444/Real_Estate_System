using otherServices.Models.DTOs.Payments;

namespace otherServices.Services.Payments
{
    public interface IPaymentFlowService
    {
        Task<object> BuyPostAsync(long userId, BuyPostRequestDto dto);

        Task<object> AcceptProposalAndStartAsync(long landlordUserId, AcceptProposalPayRequestDto dto);

        Task<object> PayRemainingAsync(PayRemainingRequestDto dto);
        Task<object> SubscribeProAsync(long landlordUserId, SubscribeProRequestDto dto);

    }
}
