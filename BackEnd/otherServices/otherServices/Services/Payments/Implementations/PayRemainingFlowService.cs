using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Configuration;
using otherServices.Models;
using otherServices.Models.DTOs.Payments;
using otherServices.Models.Enums;
using otherServices.Repositories;
using otherServices.Services;
using otherServices.Services.Interfaces;
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
        private readonly IConfiguration _cfg;
        private readonly INotificationService _notificationService;
        public PayRemainingFlowService(
            IUnitOfWork uow,
            IEncryptionService enc,
            IMockBankCardVault bank,
            IPaymentFlowHelpers helpers,
            IConfiguration cfg,
            INotificationService notificationService) 
        {
            _uow = uow;
            _enc = enc;
            _bank = bank;
            _h = helpers;
            _cfg = cfg;
            _notificationService = notificationService; 
        }

        public async Task<object> ExecuteAsync(PayRemainingRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ExternalRef))
                return new { success = false, message = "ExternalRef is required" };

            if (dto.PaymentPlanId == null && dto.PaymentScheduleId == null)
                return new { success = false, message = "PaymentPlanId or PaymentScheduleId is required" };

            var paymentCard = await _uow.PaymentCards.GetByIdAsync(dto.PaymentCardId);
            if (paymentCard == null || !paymentCard.IsActive)
                return new { success = false, message = "Payment card not found/active" };

            if (string.IsNullOrWhiteSpace(dto.CVV))
                return new { success = false, message = "CVV is required" };

            // 1) Determine targets
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

            // ✅ CONTRACT GATE (REQUIRED)
            if (planOfFirst.ContractId == null)
                return new { success = false, message = "This plan is not linked to a contract. Payment is forbidden." };

            var gate = await VerifyContractGateAsync(planOfFirst.ContractId.Value);
            if (!gate.Success)
                return new { success = false, message = gate.Message, details = gate.Details };

            var primaryScheduleId = first.PaymentScheduleId;

            long? proposalId = null;
            var approvedProposal = await _uow.Proposals.FirstOrDefaultAsync(p =>
                p.PostId == planOfFirst.PostId &&
                p.TenantId == planOfFirst.PayerUserId &&
                p.ProposalStatus == ProposalStatus.Approved);

            proposalId = approvedProposal?.ProposalId;

            decimal totalAmount = targets.Sum(x => x.Amount);

            // 2) Idempotency + Retry logic
            var existing = await _uow.Transactions.FirstOrDefaultAsync(t => t.ExternalRef == dto.ExternalRef);
            Transaction tx;

            if (existing != null)
            {
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

                if (dto.PaymentScheduleId.HasValue && existing.PaymentScheduleId.HasValue)
                {
                    if (existing.PaymentScheduleId.Value != dto.PaymentScheduleId.Value)
                        return new { success = false, message = "ExternalRef belongs to another schedule. Use a new ExternalRef." };
                }

                if (existing.UserId != planOfFirst.PayerUserId || existing.PostId != planOfFirst.PostId)
                    return new { success = false, message = "ExternalRef is not valid for this payer/post" };

                tx = existing;

                tx.Amount = totalAmount;
                tx.PaymentCardId = dto.PaymentCardId;
                tx.PaymentScheduleId = primaryScheduleId;
                tx.ProposalId = proposalId;

                tx.State = TransactionState.Pending;
                tx.LastError = null;

                tx.Status = TransactionStatus.installment;
                tx.Kind = TransactionKind.InstallmentPayment;

                // ✅ link to contract always
                tx.ContractId = planOfFirst.ContractId;
                tx.ContractHash = planOfFirst.ContractHash;

                _uow.Transactions.Update(tx);
                await _uow.CompleteAsync();
            }
            else
            {
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
                    PaymentScheduleId = primaryScheduleId,

                    // ✅ contract link
                    ContractId = planOfFirst.ContractId,
                    ContractHash = planOfFirst.ContractHash
                };

                await _uow.Transactions.AddAsync(tx);
                await _uow.CompleteAsync();
            }

            // 3) Perform transfer
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
                    tx.LastError = res.Message;
                    tx.State = TransactionState.Failed;
                    _uow.Transactions.Update(tx);

                    foreach (var s in targets)
                    {
                        s.FailedAttempts += 1;
                        s.LastFailureAt = DateTime.UtcNow;
                        s.LastError = res.Message;
                        s.NextRetryAt = DateTime.UtcNow.AddHours(12);
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

                //  Notifications after successful payment

                // Tenant
                await _notificationService.SendNotificationAsync(
                    userId: planOfFirst.PayerUserId,
                    title: "تم الدفع بنجاح 💰",
                    content: $"تم دفع القسط بنجاح بمبلغ {totalAmount}، رقم العملية {tx.TransactionId}.",
                    type: NotificationType.PaymentSuccessful,
                    targetUrl: $"/payments/{tx.TransactionId}"
                );

                // Landlord
                await _notificationService.SendNotificationAsync(
                    userId: planOfFirst.PayeeUserId,
                    title: "تم استلام دفعة 💵",
                    content: $"تم استلام مبلغ {net} في حسابك كقسط من المستخدم رقم {planOfFirst.PayerUserId}.",
                    type: NotificationType.PaymentSuccessful,
                    targetUrl: $"/payments/{tx.TransactionId}"
                );

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

        // ✅ same gate as finalize service
        private async Task<(bool Success, string Message, object? Details)> VerifyContractGateAsync(long contractId)
        {
            var contract = await _uow.Contracts.GetByIdAsync(contractId);
            if (contract == null)
                return (false, "Contract not found", null);

            if (contract.Status != ContractStatus.FullySigned)
                return (false, "Contract is not fully signed", new { status = contract.Status.ToString() });

            var secret = _cfg["Contracts:ServerSigningSecret"];
            if (string.IsNullOrWhiteSpace(secret))
                return (false, "Contracts:ServerSigningSecret not configured", null);

            var recomputed = Sha256Hex(contract.ContractJson);
            var integrityOk = string.Equals(recomputed, contract.ContractHash, StringComparison.OrdinalIgnoreCase);

            var sigs = await _uow.ContractSignatures.FindAsync(s => s.ContractId == contractId);

            bool VerifySig(ContractSignature s)
            {
                if (string.IsNullOrWhiteSpace(s.SignedPayload)) return false;
                var expected = HmacBase64(secret, s.SignedPayload);
                return string.Equals(expected, s.SignatureValue, StringComparison.Ordinal);
            }

            var buyerOk = sigs.Where(x => x.SignerRole == SignerRole.Buyer).Any(x => VerifySig(x));
            var sellerOk = sigs.Where(x => x.SignerRole == SignerRole.Seller).Any(x => VerifySig(x));

            var fullyVerifiable = integrityOk && buyerOk && sellerOk;

            if (!fullyVerifiable)
            {
                return (false, "Contract verification failed", new
                {
                    integrityOk,
                    buyerOk,
                    sellerOk,
                    signaturesCount = sigs.Count()
                });
            }

            return (true, "OK", new { integrityOk, buyerOk, sellerOk });
        }

        private static string Sha256Hex(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static string HmacBase64(string secret, string payload)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToBase64String(bytes);
        }
    }
}
