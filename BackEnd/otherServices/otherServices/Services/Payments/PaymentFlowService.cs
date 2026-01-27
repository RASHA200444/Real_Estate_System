using otherServices.Models.DTOs.Payments;
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

        // ✅ SALE - CASH
        public Task<object> SaleCashAsync(long buyerUserId, SaleCashRequestDto dto)
            => _saleCash.ExecuteAsync(buyerUserId, dto);

        // ✅ SALE - INSTALLMENT
        public Task<object> SaleInstallmentAsync(long buyerUserId, SaleInstallmentRequestDto dto)
            => _saleInstallment.ExecuteAsync(buyerUserId, dto);

        // ✅ RENT - START (accept winner + pay first month + create schedule/contract)
        public Task<object> RentStartAsync(long landlordUserId, RentStartPaymentRequestDto dto)
            => _rentStart.ExecuteAsync(landlordUserId, dto);

        // ✅ PAY REMAINING (single schedule OR all remaining in plan)
        public Task<object> PayRemainingAsync(PayRemainingRequestDto dto)
            => _payRemaining.ExecuteAsync(dto);

        // ✅ SUBSCRIBE PRO
        public Task<object> SubscribeProAsync(long landlordUserId, SubscribeProRequestDto dto)
            => _subscribePro.ExecuteAsync(landlordUserId, dto);
    }
}
