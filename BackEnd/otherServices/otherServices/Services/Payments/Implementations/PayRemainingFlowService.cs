using otherServices.Models;
using otherServices.Models.DTOs.Payments;
using otherServices.Models.Enums;
using otherServices.Repositories;
using otherServices.Services;
using otherServices.Services.Payments.Flows;
using otherServices.Services.Payments.Helpers;

namespace otherServices.Services.Payments.Implementations
{
    public class PayRemainingFlowService : IPayRemainingFlowService
    {
        private readonly IUnitOfWork _uow;
        private readonly IEncryptionService _enc;
        private readonly IMockBankCardVault _bank;
        private readonly IPaymentFlowHelpers _h;

        public PayRemainingFlowService(
            IUnitOfWork uow,
            IEncryptionService enc,
            IMockBankCardVault bank,
            IPaymentFlowHelpers helpers)
        {
            _uow = uow;
            _enc = enc;
            _bank = bank;
            _h = helpers;
        }

        public async Task<object> ExecuteAsync(PayRemainingRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ExternalRef))
                return new { success = false, message = "ExternalRef is required" };

            if (dto.PaymentPlanId == null && dto.PaymentScheduleId == null)
                return new { success = false, message = "PaymentPlanId or PaymentScheduleId is required" };

            // ✅ لازم كارت
            var paymentCard = await _uow.PaymentCards.GetByIdAsync(dto.PaymentCardId);
            if (paymentCard == null || !paymentCard.IsActive)
                return new { success = false, message = "Payment card not found/active" };

            if (string.IsNullOrWhiteSpace(dto.CVV))
                return new { success = false, message = "CVV is required" };

            // =========================
            // 1) Determine targets (schedules) to pay
            // =========================
            List<PaymentSchedule> targets = new();

            if (dto.PaymentScheduleId.HasValue)
            {
                // ✅ Pay single schedule
                var s = await _uow.PaymentSchedules.GetByIdAsync(dto.PaymentScheduleId.Value);
                if (s == null) return new { success = false, message = "PaymentSchedule not found" };
                if (s.IsPaid) return new { success = true, message = "Already paid" };
                targets.Add(s);
            }
            else
            {
                // ✅ Pay all remaining schedules for a plan
                var plan = await _uow.PaymentPlans.GetByIdAsync(dto.PaymentPlanId!.Value);
                if (plan == null) return new { success = false, message = "PaymentPlan not found" };
                if (plan.Status != PlanStatus.Active) return new { success = false, message = "Plan not active" };

                // ✅ منع الدفع بكارت مختلف عن المرتبط بالخطة (زي ما كان عندك)
                if (plan.PaymentCardId != dto.PaymentCardId)
                    return new { success = false, message = "This plan is linked to another card. Use the linked card." };

                var due = await _uow.PaymentSchedules.FindAsync(s => s.PaymentPlanId == plan.PaymentPlanId && !s.IsPaid);
                targets = due.OrderBy(x => x.DueDate).ToList();

                if (!targets.Any())
                    return new { success = true, message = "Nothing remaining to pay" };
            }

            var first = targets[0];

            var planOfFirst = await _uow.PaymentPlans.GetByIdAsync(first.PaymentPlanId);
            if (planOfFirst == null) return new { success = false, message = "Plan missing" };

            // ✅ ownership check: الكارت لازم بتاع الـ payer
            if (paymentCard.UserId != planOfFirst.PayerUserId)
                return new { success = false, message = "You do not own this card / payer mismatch" };

            // ✅ احنا هنضمن إن Transaction.PaymentScheduleId مايبقاش null:
            // - لو دفعة واحدة (schedule واحدة) => نفس id
            // - لو batch (كل المتبقي في الخطة) => نحط "أول Schedule" كـ primary reference
            var primaryScheduleId = first.PaymentScheduleId;

            // ✅ Try attach ProposalId automatically (if exists)
            long? proposalId = null;
            var approvedProposal = await _uow.Proposals.FirstOrDefaultAsync(p =>
                p.PostId == planOfFirst.PostId &&
                p.TenantId == planOfFirst.PayerUserId &&
                p.ProposalStatus == ProposalStatus.Approved);

            proposalId = approvedProposal?.ProposalId;

            decimal totalAmount = targets.Sum(x => x.Amount);

            // =========================
            // 2) Idempotency + Retry logic
            // =========================
            // الفكرة هنا:
            // - لو ExternalRef موجود و SUCCEEDED => خلاص متعملش خصم تاني
            // - لو ExternalRef موجود و FAILED => "Retry" على نفس الـ row (نفس TransactionId) بدل إنشاء row جديد
            //   بشرط منطقي: نفس schedule (لو dto.PaymentScheduleId اتبعت) أو على الأقل نفس payer/post
            // =========================
            var existing = await _uow.Transactions.FirstOrDefaultAsync(t => t.ExternalRef == dto.ExternalRef);

            Transaction tx;

            if (existing != null)
            {
                // ✅ لو نجحت قبل كده => مانعيدش الخصم
                if (existing.State == TransactionState.Succeeded)
                {
                    return new
                    {
                        success = true,
                        message = "Already processed",
                        transactionId = existing.TransactionId,
                        amount = existing.Amount
                    };
                }

                // ✅ لو المستخدم بيحاول يعمل Retry بنفس ExternalRef:
                // نضمن إنه بيحاول على نفس schedule (لو كان request محدد schedule)
                if (dto.PaymentScheduleId.HasValue && existing.PaymentScheduleId.HasValue)
                {
                    if (existing.PaymentScheduleId.Value != dto.PaymentScheduleId.Value)
                        return new { success = false, message = "ExternalRef belongs to another schedule. Use a new ExternalRef." };
                }

                // ✅ منع حد يستخدم ExternalRef قديم لدفع حاجة تانية لشخص تاني
                if (existing.UserId != planOfFirst.PayerUserId || existing.PostId != planOfFirst.PostId)
                    return new { success = false, message = "ExternalRef is not valid for this payer/post" };

                // ✅ reuse same tx row (retry)
                tx = existing;

                // مهم: update data to current attempt (amount/schedule)
                tx.Amount = totalAmount;
                tx.PaymentCardId = dto.PaymentCardId;
                tx.PaymentScheduleId = primaryScheduleId; // ✅ مايبقاش null
                tx.ProposalId = proposalId;

                // reset state for retry
                tx.State = TransactionState.Pending;
                tx.LastError = null;

                // نعتبره installment (حسب الكود اللي عندك)
                tx.Status = TransactionStatus.installment;
                tx.Kind = TransactionKind.InstallmentPayment;

                _uow.Transactions.Update(tx);
                await _uow.CompleteAsync();
            }
            else
            {
                // ✅ First time for this ExternalRef => create tx
                tx = new Transaction
                {
                    UserId = planOfFirst.PayerUserId,
                    PostId = planOfFirst.PostId,
                    ProposalId = proposalId,
                    Amount = totalAmount,
                    PaymentMethod = "Card",
                    Status = TransactionStatus.installment,
                    State = TransactionState.Pending,
                    Kind = TransactionKind.InstallmentPayment,
                    ExternalRef = dto.ExternalRef,
                    PaymentCardId = dto.PaymentCardId,
                    Attempts = 0,
                    CreatedAt = DateTime.UtcNow,

                    // ✅ IMPORTANT: لا تتركها null
                    PaymentScheduleId = primaryScheduleId
                };

                await _uow.Transactions.AddAsync(tx);
                await _uow.CompleteAsync();
            }

            // =========================
            // 3) Perform transfer
            // =========================
            try
            {
                // ✅ count attempts لكل مرة (حتى الريتراي)
                tx.Attempts += 1;
                _uow.Transactions.Update(tx);
                await _uow.CompleteAsync();

                var adminUserId = _h.GetAdminUserId();
                var landlordUserId = planOfFirst.PayeeUserId;

                var landlordCard = await _h.GetDefaultActiveCardAsync(landlordUserId);
                if (landlordCard == null) return new { success = false, message = "Landlord has no active payment card" };

                var adminCard = await _h.GetDefaultActiveCardAsync(adminUserId);
                if (adminCard == null) return new { success = false, message = "Admin has no active payment card" };

                var payerToken = _enc.Decrypt(paymentCard.CardTokenEncrypted);
                var payeeToken = _enc.Decrypt(landlordCard.CardTokenEncrypted);
                var adminToken = _enc.Decrypt(adminCard.CardTokenEncrypted);

                var feePercent = _h.GetFeePercent();
                var fee = Math.Round(totalAmount * feePercent / 100m, 2);
                var net = totalAmount - fee;

                var res = await _bank.TransferWithFeeAsync(
                    payerToken: payerToken,
                    cvv: dto.CVV,
                    amount: totalAmount,
                    payeeToken: payeeToken,
                    adminToken: adminToken,
                    feeAmount: fee
                );

                tx.FeeAmount = fee;
                tx.NetToLandlord = net;
                tx.LandlordUserId = landlordUserId;
                tx.AdminUserId = adminUserId;

                if (!res.Success)
                {
                    // ✅ FAIL: transaction failed, schedules remain unpaid + set retry metadata
                    tx.LastError = res.Message;
                    tx.State = TransactionState.Failed;
                    _uow.Transactions.Update(tx);

                    foreach (var s in targets)
                    {
                        s.FailedAttempts += 1;
                        s.LastFailureAt = DateTime.UtcNow;
                        s.LastError = res.Message;
                        s.NextRetryAt = DateTime.UtcNow.AddHours(12);

                        // ✅ IMPORTANT: ربط الـ schedule بالـ tx حتى لو فشل (للتتبع)
                        s.TransactionId = tx.TransactionId;

                        _uow.PaymentSchedules.Update(s);
                    }

                    await _uow.CompleteAsync();

                    return new
                    {
                        success = false,
                        message = $"Payment failed: {res.Message}",
                        transactionId = tx.TransactionId,
                        amount = totalAmount,
                        fee,
                        net,
                        primaryScheduleId
                    };
                }

                // ✅ SUCCESS
                tx.State = TransactionState.Succeeded;
                tx.LastError = null;
                _uow.Transactions.Update(tx);

                foreach (var s in targets)
                {
                    s.IsPaid = true;
                    s.PaidAt = DateTime.UtcNow;
                    s.TransactionId = tx.TransactionId;

                    // clear failures
                    s.LastError = null;
                    s.NextRetryAt = null;

                    _uow.PaymentSchedules.Update(s);
                }

                await _uow.CompleteAsync();

                // ✅ if nothing remaining => complete plan + maybe mark post sold
                var remaining = await _uow.PaymentSchedules.FindAsync(s => s.PaymentPlanId == planOfFirst.PaymentPlanId && !s.IsPaid);
                if (!remaining.Any())
                {
                    planOfFirst.Status = PlanStatus.Completed;
                    _uow.PaymentPlans.Update(planOfFirst);

                    if (planOfFirst.PropertyType == PropertyType.Sale && planOfFirst.IsInstallment == IsInstallment.Installment)
                    {
                        var post = await _uow.Posts.GetByIdAsync(planOfFirst.PostId);
                        if (post != null)
                        {
                            post.Status = PropertyStatus.Sold;
                            _uow.Posts.Update(post);
                        }
                    }

                    await _uow.CompleteAsync();
                }

                return new
                {
                    success = true,
                    message = "Paid",
                    transactionId = tx.TransactionId,
                    amount = totalAmount,
                    fee,
                    net,
                    proposalId,
                    primaryScheduleId
                };
            }
            catch (Exception ex)
            {
                // ✅ SAFETY: mark tx failed (same row) + keep ability to retry with same ExternalRef
                var err = ex.InnerException?.Message ?? ex.Message;

                tx.LastError = err;
                tx.State = TransactionState.Failed;
                _uow.Transactions.Update(tx);

                foreach (var s in targets)
                {
                    s.FailedAttempts += 1;
                    s.LastFailureAt = DateTime.UtcNow;
                    s.LastError = err;
                    s.NextRetryAt = DateTime.UtcNow.AddHours(12);

                    // ✅ link schedule to tx for traceability
                    s.TransactionId = tx.TransactionId;

                    _uow.PaymentSchedules.Update(s);
                }

                await _uow.CompleteAsync();

                return new
                {
                    success = false,
                    message = "Payment error (safe). Retry with SAME ExternalRef.",
                    transactionId = tx.TransactionId,
                    primaryScheduleId
                };
            }
        }
    }
}
