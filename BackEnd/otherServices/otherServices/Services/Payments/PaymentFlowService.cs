using otherServices.Models.DTOs.Payments;
using otherServices.Models.Enums;
using otherServices.Repositories;
using otherServices.Services.Payments.Flows;

namespace otherServices.Services.Payments
{
    public class PaymentFlowService : IPaymentFlowService
    {
        private readonly IUnitOfWork _uow;
        private readonly ISaleCashFlowService _saleCash;
        private readonly ISaleInstallmentFlowService _saleInstallment;
        private readonly IRentStartFlowService _rentStart;
        private readonly IPayRemainingFlowService _payRemaining;

        // ✅ NEW
        private readonly ISubscribeProFlowService _subscribePro;

        public PaymentFlowService(
            IUnitOfWork uow,
            ISaleCashFlowService saleCash,
            ISaleInstallmentFlowService saleInstallment,
            IRentStartFlowService rentStart,
            IPayRemainingFlowService payRemaining,
            ISubscribeProFlowService subscribePro)
        {
            _uow = uow;
            _saleCash = saleCash;
            _saleInstallment = saleInstallment;
            _rentStart = rentStart;
            _payRemaining = payRemaining;
            _subscribePro = subscribePro;
        }

        public async Task<object> BuyPostAsync(long userId, BuyPostRequestDto dto)
        {
            if (dto.IsInstallment == IsInstallment.Cash)
                return await _saleCash.ExecuteAsync(userId, dto);

            return await _saleInstallment.ExecuteAsync(userId, dto);
        }

        public Task<object> AcceptProposalAndStartAsync(long landlordUserId, AcceptProposalPayRequestDto dto)
            => _rentStart.ExecuteAsync(landlordUserId, dto);

        public Task<object> PayRemainingAsync(PayRemainingRequestDto dto)
            => _payRemaining.ExecuteAsync(dto);

        // ✅ NEW
        public Task<object> SubscribeProAsync(long landlordUserId, SubscribeProRequestDto dto)
            => _subscribePro.ExecuteAsync(landlordUserId, dto);
    }
}
