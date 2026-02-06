using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otherServices.Models.DTOs.Payments;
using otherServices.Models.Enums;
using otherServices.Repositories;
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
        private readonly IUnitOfWork _uow;

        public PaymentsFlowController(
            ISaleCashFlowService saleCash,
            ISaleInstallmentFlowService saleInstallment,
            IRentStartFlowService rentStart,
            IPayRemainingFlowService payRemaining,
            IPaymentFinalizeService finalize,
            IUnitOfWork uow)
        {
            _saleCash = saleCash;
            _saleInstallment = saleInstallment;
            _rentStart = rentStart;
            _payRemaining = payRemaining;
            _finalize = finalize;
            _uow = uow;
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
        [HttpPost("rent/start/{tenantUserId}")]
        public async Task<IActionResult> RentStart(long tenantUserId, [FromBody] RentStartPaymentRequestDto dto)
            => Ok(await _rentStart.ExecuteAsync(tenantUserId, dto));

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
            var requesterUserId = GetRequesterUserId();
            if (requesterUserId == null)
                return Unauthorized(new { success = false, message = "Invalid token (missing userId)" });

            return Ok(await _finalize.FinalizeAsync(requesterUserId.Value, dto));
        }

        // ✅ NEW: GET TX BY CONTRACT (to fetch ExternalRef for finalize from ContractDetails page)
        // GET /api/payments/tx/by-contract/{contractId}
        [HttpGet("tx/by-contract/{contractId:long}")]
        [Authorize]
        public async Task<IActionResult> GetTxByContract(long contractId)
        {
            var requesterUserId = GetRequesterUserId();
            if (requesterUserId == null)
                return Unauthorized(new { success = false, message = "Invalid token (missing userId)" });

            // ✅ ensure requester is participant in this contract (tenant or landlord)
            var contract = await _uow.Contracts.GetByIdAsync(contractId);
            if (contract == null)
                return NotFound(new { success = false, message = "Contract not found" });

            var isParticipant =
                contract.TenantId == requesterUserId.Value ||
                contract.LandlordUserId == requesterUserId.Value;

            if (!isParticipant)
                return Forbid();

            // ✅ get related transaction
            var tx = await _uow.Transactions.FirstOrDefaultAsync(t => t.ContractId == contractId);
            if (tx == null)
                return NotFound(new { success = false, message = "No transaction found for this contract" });

            return Ok(new
            {
                success = true,
                contractId,
                transactionId = tx.TransactionId,
                externalRef = tx.ExternalRef,
                state = tx.State.ToString(),
                kind = tx.Kind.ToString(),
                amount = tx.Amount,
                contractStatus = contract.Status.ToString(),
                finalizeAllowed = contract.Status == ContractStatus.FullySigned && tx.State != TransactionState.Succeeded
            });
        }

        // =========================
        // Helpers
        // =========================
        private long? GetRequesterUserId()
        {
            var requesterUserIdStr =
                User.Claims.FirstOrDefault(c => c.Type == "uid")?.Value
                ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                ?? User.Claims.FirstOrDefault(c => c.Type.EndsWith("/nameidentifier"))?.Value;

            if (string.IsNullOrWhiteSpace(requesterUserIdStr) || !long.TryParse(requesterUserIdStr, out var requesterUserId))
                return null;

            return requesterUserId;
        }
    }
}
