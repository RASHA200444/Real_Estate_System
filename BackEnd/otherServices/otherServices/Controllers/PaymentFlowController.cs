// ===============================
// File: otherServices/Controllers/Payments/PaymentsFlowController.cs
// ===============================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otherServices.Models.DTOs.Payments;
using otherServices.Models.Enums;
using otherServices.Repositories;
using otherServices.Services.Payments.Finalize;
using otherServices.Services.Payments.Flows;

namespace otherServices.Controllers.Payments
{
    [Route("api/payments")]
    [ApiController]
    [Authorize] // تأمين كل عمليات الدفع بشكل افتراضي
    public class PaymentsFlowController : BaseApiController // الوراثة من الكلاس الموحد
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

        // ✅ SALE - CASH
        // تم حذف {buyerUserId} من الـ Route واعتماده من التوكن
        [HttpPost("sale/cash")]
        public async Task<IActionResult> SaleCash([FromBody] SaleCashRequestDto dto)
        {
            if (RequireUserId(out var buyerUserId) is IActionResult error) return error;
            return Ok(await _saleCash.ExecuteAsync(buyerUserId, dto));
        }

        // ✅ SALE - INSTALLMENT
        [HttpPost("sale/installment")]
        public async Task<IActionResult> SaleInstallment([FromBody] SaleInstallmentRequestDto dto)
        {
            if (RequireUserId(out var buyerUserId) is IActionResult error) return error;
            return Ok(await _saleInstallment.ExecuteAsync(buyerUserId, dto));
        }

        // ✅ RENT - START
        [HttpPost("rent/start")]
        public async Task<IActionResult> RentStart([FromBody] RentStartPaymentRequestDto dto)
        {
            if (RequireUserId(out var tenantUserId) is IActionResult error) return error;
            return Ok(await _rentStart.ExecuteAsync(tenantUserId, dto));
        }

        // ✅ PAY REMAINING
        [HttpPost("remaining/pay")]
        public async Task<IActionResult> PayRemaining([FromBody] PayRemainingRequestDto dto)
        {
            // حتى لو الميثود مش محتاجة ID صريح، الـ Authorize فوق بيضمن وجود مستخدم
            return Ok(await _payRemaining.ExecuteAsync(dto));
        }

        // ✅ FINALIZE
        [HttpPost("finalize")]
        public async Task<IActionResult> Finalize([FromBody] FinalizePaymentDto dto)
        {
            if (RequireUserId(out var requesterUserId) is IActionResult error) return error;
            return Ok(await _finalize.FinalizeAsync(requesterUserId, dto));
        }

        // ✅ GET TX BY CONTRACT
        [HttpGet("tx/by-contract/{contractId:long}")]
        public async Task<IActionResult> GetTxByContract(long contractId)
        {
            if (RequireUserId(out var requesterUserId) is IActionResult error) return error;

            var contract = await _uow.Contracts.GetByIdAsync(contractId);
            if (contract == null)
                return NotFound(new { success = false, message = "Contract not found" });

            // التأكد أن المستخدم طرف في العقد
            var isParticipant =
                contract.TenantId == requesterUserId ||
                contract.LandlordUserId == requesterUserId;

            if (!isParticipant)
                return Forbid();

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
    }
}