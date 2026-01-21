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

            // idempotency
            var existing = await _uow.Transactions.FirstOrDefaultAsync(t => t.ExternalRef == dto.ExternalRef);
            if (existing != null)
                return new { success = true, message = "Already processed", transactionId = existing.TransactionId, amount = existing.Amount };

            var paymentCard = await _uow.PaymentCards.GetByIdAsync(dto.PaymentCardId);
            if (paymentCard == null || !paymentCard.IsActive)
                return new { success = false, message = "Payment card not found/active" };

            if (string.IsNullOrWhiteSpace(dto.CVV))
                return new { success = false, message = "CVV is required" };

            List<PaymentSchedule> targets = new();

            if (dto.PaymentScheduleId.HasValue)
            {
                var s = await _uow.PaymentSchedules.GetByIdAsync(dto.PaymentScheduleId.Value);
                if (s == null) return new { success = false, message = "PaymentSchedule not found" };
                if (s.IsPaid) return new { success = true, message = "Already paid" };
                targets.Add(s);
            }
            else
            {
                var plan = await _uow.PaymentPlans.GetByIdAsync(dto.PaymentPlanId!.Value);
                if (plan == null) return new { success = false, message = "PaymentPlan not found" };
                if (plan.Status != PlanStatus.Active) return new { success = false, message = "Plan not active" };

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

            // ownership check
            if (paymentCard.UserId != planOfFirst.PayerUserId)
                return new { success = false, message = "You do not own this card / payer mismatch" };

            // ✅ Try attach ProposalId automatically (if exists)
            long? proposalId = null;

            // PostId هنا long مش nullable
            var postId = planOfFirst.PostId;

            var approvedProposal = await _uow.Proposals.FirstOrDefaultAsync(p =>
                p.PostId == postId &&
                p.TenantId == planOfFirst.PayerUserId &&
                p.ProposalStatus == ProposalStatus.Approved);

            proposalId = approvedProposal?.ProposalId;


            decimal totalAmount = targets.Sum(x => x.Amount);

            var tx = new Transaction
            {
                UserId = planOfFirst.PayerUserId,
                PostId = planOfFirst.PostId,
                ProposalId = proposalId, // ✅ NEW
                Amount = totalAmount,
                PaymentMethod = "Card",
                Status = TransactionStatus.installment,
                State = TransactionState.Pending,
                Kind = TransactionKind.InstallmentPayment,
                ExternalRef = dto.ExternalRef,
                PaymentCardId = dto.PaymentCardId,
                Attempts = 0,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Transactions.AddAsync(tx);
            await _uow.CompleteAsync();

            try
            {
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

                var res = await _bank.TransferWithFeeAsync(payerToken, dto.CVV, totalAmount, payeeToken, adminToken, fee);

                tx.FeeAmount = fee;
                tx.NetToLandlord = net;
                tx.LandlordUserId = landlordUserId;
                tx.AdminUserId = adminUserId;

                if (!res.Success)
                {
                    tx.LastError = res.Message;
                    tx.State = TransactionState.Failed;
                    _uow.Transactions.Update(tx);

                    foreach (var s in targets)
                    {
                        s.FailedAttempts += 1;
                        s.LastFailureAt = DateTime.UtcNow;
                        s.LastError = res.Message;
                        s.NextRetryAt = DateTime.UtcNow.AddHours(12);
                        _uow.PaymentSchedules.Update(s);
                    }

                    await _uow.CompleteAsync();
                    return new { success = false, message = $"Payment failed: {res.Message}", transactionId = tx.TransactionId };
                }

                tx.State = TransactionState.Succeeded;
                tx.LastError = null;
                _uow.Transactions.Update(tx);

                foreach (var s in targets)
                {
                    s.IsPaid = true;
                    s.PaidAt = DateTime.UtcNow;
                    s.TransactionId = tx.TransactionId;
                    s.LastError = null;
                    s.NextRetryAt = null;
                    _uow.PaymentSchedules.Update(s);
                }

                await _uow.CompleteAsync();

                // if no remaining => complete plan + maybe post Sold
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

                return new { success = true, message = "Paid", transactionId = tx.TransactionId, amount = totalAmount, fee, net, proposalId };
            }
            catch (Exception ex)
            {
                var err = ex.InnerException?.Message ?? ex.Message;
                tx.LastError = err;
                tx.State = TransactionState.Failed;
                _uow.Transactions.Update(tx);
                await _uow.CompleteAsync();

                return new { success = false, message = "Payment error (safe). Retry with SAME ExternalRef.", transactionId = tx.TransactionId };
            }
        }
    }
}
