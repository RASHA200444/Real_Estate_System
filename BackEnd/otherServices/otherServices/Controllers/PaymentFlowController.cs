using Microsoft.AspNetCore.Mvc;
using otherServices.Models.DTOs.Payments;
using otherServices.Services.Payments.Flows;

namespace otherServices.Controllers.Payments
{
    [Route("api/payments")]
    [ApiController]
    public class PaymentsFlowController : ControllerBase
    {
        private readonly ISaleCashFlowService _saleCash;
        private readonly ISaleInstallmentFlowService _saleInstallment;
        private readonly IRentStartFlowService _rentStart;
        private readonly IPayRemainingFlowService _payRemaining;

        public PaymentsFlowController(
            ISaleCashFlowService saleCash,
            ISaleInstallmentFlowService saleInstallment,
            IRentStartFlowService rentStart,
            IPayRemainingFlowService payRemaining)
        {
            _saleCash = saleCash;
            _saleInstallment = saleInstallment;
            _rentStart = rentStart;
            _payRemaining = payRemaining;
        }

        // ✅ SALE - CASH
        [HttpPost("sale/cash/{buyerUserId}")]
        public async Task<IActionResult> SaleCash(long buyerUserId, [FromBody] SaleCashRequestDto dto)
            => Ok(await _saleCash.ExecuteAsync(buyerUserId, dto));

        // ✅ SALE - INSTALLMENT
        [HttpPost("sale/installment/{buyerUserId}")]
        public async Task<IActionResult> SaleInstallment(long buyerUserId, [FromBody] SaleInstallmentRequestDto dto)
            => Ok(await _saleInstallment.ExecuteAsync(buyerUserId, dto));

        // ✅ RENT - START (landlord accepts winner + pays first month)
        [HttpPost("rent/start/{landlordUserId}")]
        public async Task<IActionResult> RentStart(long landlordUserId, [FromBody] RentStartPaymentRequestDto dto)
            => Ok(await _rentStart.ExecuteAsync(landlordUserId, dto));

        // ✅ PAY REMAINING (plan or schedule)
        [HttpPost("remaining/pay")]
        public async Task<IActionResult> PayRemaining([FromBody] PayRemainingRequestDto dto)
            => Ok(await _payRemaining.ExecuteAsync(dto));
    }
}
