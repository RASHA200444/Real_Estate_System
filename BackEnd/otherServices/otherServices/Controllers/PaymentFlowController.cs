using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otherServices.Models.DTOs.Payments;
using otherServices.Services.Payments.Finalize;
using otherServices.Services.Payments.Flows;
using System.Security.Claims;

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
        private readonly IPaymentFinalizeService _finalize;

        public PaymentsFlowController(
            ISaleCashFlowService saleCash,
            ISaleInstallmentFlowService saleInstallment,
            IRentStartFlowService rentStart,
            IPayRemainingFlowService payRemaining,
            IPaymentFinalizeService finalize)
        {
            _saleCash = saleCash;
            _saleInstallment = saleInstallment;
            _rentStart = rentStart;
            _payRemaining = payRemaining;
            _finalize = finalize;
        }

        // ✅ SALE - CASH (NOW: Initiate -> creates contract+tx awaiting signatures, NO transfer)
        [HttpPost("sale/cash/{buyerUserId}")]
        public async Task<IActionResult> SaleCash(long buyerUserId, [FromBody] SaleCashRequestDto dto)
            => Ok(await _saleCash.ExecuteAsync(buyerUserId, dto));

        // ✅ SALE - INSTALLMENT (NOW: Initiate -> creates plan+first schedule+contract+tx awaiting signatures, NO transfer)
        [HttpPost("sale/installment/{buyerUserId}")]
        public async Task<IActionResult> SaleInstallment(long buyerUserId, [FromBody] SaleInstallmentRequestDto dto)
            => Ok(await _saleInstallment.ExecuteAsync(buyerUserId, dto));

        // ✅ RENT - START (NOW: Initiate -> creates plan+first schedule+contract+tx awaiting signatures, NO transfer)
        [HttpPost("rent/start/{landlordUserId}")]
        public async Task<IActionResult> RentStart(long landlordUserId, [FromBody] RentStartPaymentRequestDto dto)
            => Ok(await _rentStart.ExecuteAsync(landlordUserId, dto));

        // ✅ PAY REMAINING (MODIFIED: must verify signed contract before transfer)
        [HttpPost("remaining/pay")]
        public async Task<IActionResult> PayRemaining([FromBody] PayRemainingRequestDto dto)
            => Ok(await _payRemaining.ExecuteAsync(dto));

        // ✅ NEW: FINALIZE (THE ONLY PLACE WHERE MONEY MOVES)
        // Client calls this after both parties sign and contract verify is OK.
        [HttpPost("finalize")]
        [Authorize]
        public async Task<IActionResult> Finalize([FromBody] FinalizePaymentDto dto)
        {
            var requesterUserIdStr =
                User.Claims.FirstOrDefault(c => c.Type == "uid")?.Value
                ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                ?? User.Claims.FirstOrDefault(c => c.Type.EndsWith("/nameidentifier"))?.Value;

            if (string.IsNullOrWhiteSpace(requesterUserIdStr) || !long.TryParse(requesterUserIdStr, out var requesterUserId))
                return Unauthorized(new { success = false, message = "Invalid token (missing userId)" });

            return Ok(await _finalize.FinalizeAsync(requesterUserId, dto));
        }
    }
}
