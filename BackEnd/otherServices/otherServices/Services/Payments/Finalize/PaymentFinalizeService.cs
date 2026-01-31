using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using otherServices.Models;
using otherServices.Models.DTOs.Payments;
using otherServices.Models.Enums;
using otherServices.Repositories;
using otherServices.Services.Payments.Helpers;

namespace otherServices.Services.Payments.Finalize
{
    public class PaymentFinalizeService : IPaymentFinalizeService
    {
        private readonly IUnitOfWork _uow;
        private readonly IConfiguration _cfg;
        private readonly IEncryptionService _enc;
        private readonly IMockBankCardVault _bank;
        private readonly IPaymentFlowHelpers _helpers;

        public PaymentFinalizeService(
            IUnitOfWork uow,
            IConfiguration cfg,
            IEncryptionService enc,
            IMockBankCardVault bank,
            IPaymentFlowHelpers helpers)
        {
            _uow = uow;
            _cfg = cfg;
            _enc = enc;
            _bank = bank;
            _helpers = helpers;
        }

        public async Task<object> FinalizeAsync(long requesterUserId, FinalizePaymentDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ExternalRef))
                return new { success = false, message = "ExternalRef is required" };

            var tx = await _uow.Transactions.FirstOrDefaultAsync(t => t.ExternalRef == dto.ExternalRef);
            if (tx == null)
                return new { success = false, message = "Transaction not found" };

            // ✅ payer only (the one who will provide CVV)
            if (tx.UserId != requesterUserId)
                return new { success = false, message = "Not allowed. Only payer can finalize this payment." };

            // ✅ idempotent
            if (tx.State == TransactionState.Succeeded)
                return new { success = true, message = "Already paid", transactionId = tx.TransactionId };

            if (tx.ContractId == null)
                return new { success = false, message = "Transaction has no contract. Cannot finalize." };

            // ✅ THE ONLY GATE: contract must be fully signed + verifiable
            var gate = await VerifyContractGateAsync(tx.ContractId.Value);
            if (!gate.Success)
                return new { success = false, message = gate.Message, details = gate.Details };

            // Need payer card
            var payerCard = await _uow.PaymentCards.GetByIdAsync(tx.PaymentCardId ?? 0);
            if (payerCard == null || !payerCard.IsActive)
                return new { success = false, message = "Payment card not found/active" };

            // Ensure payer owns the card
            if (payerCard.UserId != tx.UserId)
                return new { success = false, message = "You do not own this payment card" };

            // Admin
            var adminUserId = _helpers.GetAdminUserId();
            var feePercent = _helpers.GetFeePercent();

            // Begin DB transaction for atomicity
            var db = TryGetDbContext();
            if (db == null)
                return new { success = false, message = "DbContext not accessible from UnitOfWork. Expose Context like UnitOfWork.Context." };

            await using var trx = await db.Database.BeginTransactionAsync();

            try
            {
                tx.Attempts += 1;
                tx.State = TransactionState.Pending;
                tx.LastError = null;
                _uow.Transactions.Update(tx);
                await _uow.CompleteAsync();

                switch (tx.Kind)
                {
                    case TransactionKind.SaleCash:
                        {
                            var res = await FinalizeSaleCashAsync(tx, payerCard, dto.CVV, feePercent, adminUserId);
                            if (!res.Success)
                            {
                                await trx.RollbackAsync();
                                return new { success = false, message = res.Message, transactionId = tx.TransactionId };
                            }

                            await trx.CommitAsync();
                            return new
                            {
                                success = true,
                                message = "Paid after verified signatures",
                                transactionId = tx.TransactionId,
                                contractId = tx.ContractId,
                                contractHash = tx.ContractHash
                            };
                        }

                    case TransactionKind.SaleInstallment:
                        {
                            var res = await FinalizeSaleInstallmentFirstPaymentAsync(tx, payerCard, dto.CVV, feePercent, adminUserId);
                            if (!res.Success)
                            {
                                await trx.RollbackAsync();
                                return new { success = false, message = res.Message, transactionId = tx.TransactionId };
                            }

                            await trx.CommitAsync();
                            return new
                            {
                                success = true,
                                message = "First installment paid after verified signatures",
                                transactionId = tx.TransactionId,
                                contractId = tx.ContractId,
                                contractHash = tx.ContractHash
                            };
                        }

                    case TransactionKind.Rent:
                        {
                            var res = await FinalizeRentStartFirstPaymentAsync(tx, payerCard, dto.CVV, feePercent, adminUserId);
                            if (!res.Success)
                            {
                                await trx.RollbackAsync();
                                return new { success = false, message = res.Message, transactionId = tx.TransactionId };
                            }

                            await trx.CommitAsync();
                            return new
                            {
                                success = true,
                                message = "Rent first month paid after verified signatures",
                                transactionId = tx.TransactionId,
                                contractId = tx.ContractId,
                                contractHash = tx.ContractHash
                            };
                        }

                    default:
                        await trx.RollbackAsync();
                        return new { success = false, message = $"Finalize not supported for kind: {tx.Kind}" };
                }
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();

                tx.State = TransactionState.Failed;
                tx.LastError = ex.InnerException?.Message ?? ex.Message;
                _uow.Transactions.Update(tx);
                await _uow.CompleteAsync();

                return new { success = false, message = "Finalize error (safe). Retry with SAME ExternalRef.", transactionId = tx.TransactionId };
            }
        }

        // =========================
        // Finalize per kind
        // =========================

        private async Task<(bool Success, string Message)> FinalizeSaleCashAsync(
            Transaction tx,
            PaymentCard payerCard,
            string cvv,
            decimal feePercent,
            long adminUserId)
        {
            if (!tx.PostId.HasValue) return (false, "Transaction missing PostId");

            // ✅ FIX: Amount is decimal (non-nullable) in your model
            if (tx.Amount <= 0) return (false, "Transaction amount invalid");

            var post = await _uow.Posts.GetByIdAsync(tx.PostId.Value);
            if (post == null) return (false, "Post not found");

            if (post.Status == PropertyStatus.Sold) return (false, "Post already sold");

            // Seller
            var landlord = await _uow.Landlords.GetByIdAsync(post.LandlordId);
            if (landlord == null) return (false, "Seller not found");

            var landlordUserId = landlord.UserId;

            var landlordCard = await _helpers.GetDefaultActiveCardAsync(landlordUserId);
            if (landlordCard == null) return (false, "Landlord has no active payment card");

            var adminCard = await _helpers.GetDefaultActiveCardAsync(adminUserId);
            if (adminCard == null) return (false, "Admin has no active payment card");

            // ✅ FIX: Amount is decimal
            var total = tx.Amount;

            var fee = Math.Round(total * feePercent / 100m, 2);
            var net = total - fee;

            var payerToken = _enc.Decrypt(payerCard.CardTokenEncrypted);
            var payeeToken = _enc.Decrypt(landlordCard.CardTokenEncrypted);
            var adminToken = _enc.Decrypt(adminCard.CardTokenEncrypted);

            var bankRes = await _bank.TransferWithFeeAsync(payerToken, cvv, total, payeeToken, adminToken, fee);

            tx.FeeAmount = fee;
            tx.NetToLandlord = net;
            tx.LandlordUserId = landlordUserId;
            tx.AdminUserId = adminUserId;

            if (!bankRes.Success)
            {
                tx.State = TransactionState.Failed;
                tx.LastError = bankRes.Message;
                _uow.Transactions.Update(tx);
                await _uow.CompleteAsync();
                return (false, $"Payment failed: {bankRes.Message}");
            }

            tx.State = TransactionState.Succeeded;
            tx.LastError = null;
            _uow.Transactions.Update(tx);

            // Mark sold now (ONLY after signed+verified+paid)
            post.Status = PropertyStatus.Sold;
            _uow.Posts.Update(post);

            await _uow.CompleteAsync();
            return (true, "OK");
        }

        private async Task<(bool Success, string Message)> FinalizeSaleInstallmentFirstPaymentAsync(
            Transaction tx,
            PaymentCard payerCard,
            string cvv,
            decimal feePercent,
            long adminUserId)
        {
            // ✅ FIX: Amount is decimal
            if (tx.Amount <= 0) return (false, "Transaction amount invalid");
            if (tx.PaymentScheduleId == null) return (false, "Missing PaymentScheduleId");

            var schedule = await _uow.PaymentSchedules.GetByIdAsync(tx.PaymentScheduleId.Value);
            if (schedule == null) return (false, "PaymentSchedule not found");

            if (schedule.IsPaid) return (true, "Already paid");

            var plan = await _uow.PaymentPlans.GetByIdAsync(schedule.PaymentPlanId);
            if (plan == null) return (false, "PaymentPlan not found");

            if (plan.Status != PlanStatus.Active) return (false, "Plan not active");

            // Ensure plan is tied to same contract
            if (plan.ContractId == null || plan.ContractId != tx.ContractId)
                return (false, "Plan contract mismatch / missing");

            // Payee
            var landlordUserId = plan.PayeeUserId;

            var landlordCard = await _helpers.GetDefaultActiveCardAsync(landlordUserId);
            if (landlordCard == null) return (false, "Landlord has no active payment card");

            var adminCard = await _helpers.GetDefaultActiveCardAsync(adminUserId);
            if (adminCard == null) return (false, "Admin has no active payment card");

            // ✅ FIX: Amount is decimal
            var amount = tx.Amount;

            var fee = Math.Round(amount * feePercent / 100m, 2);
            var net = amount - fee;

            var payerToken = _enc.Decrypt(payerCard.CardTokenEncrypted);
            var payeeToken = _enc.Decrypt(landlordCard.CardTokenEncrypted);
            var adminToken = _enc.Decrypt(adminCard.CardTokenEncrypted);

            var bankRes = await _bank.TransferWithFeeAsync(payerToken, cvv, amount, payeeToken, adminToken, fee);

            tx.FeeAmount = fee;
            tx.NetToLandlord = net;
            tx.LandlordUserId = landlordUserId;
            tx.AdminUserId = adminUserId;

            if (!bankRes.Success)
            {
                tx.State = TransactionState.Failed;
                tx.LastError = bankRes.Message;
                _uow.Transactions.Update(tx);

                schedule.FailedAttempts += 1;
                schedule.LastFailureAt = DateTime.UtcNow;
                schedule.LastError = bankRes.Message;
                schedule.NextRetryAt = DateTime.UtcNow.AddHours(12);
                schedule.TransactionId = tx.TransactionId;
                _uow.PaymentSchedules.Update(schedule);

                await _uow.CompleteAsync();
                return (false, $"Payment failed: {bankRes.Message}");
            }

            // Success
            tx.State = TransactionState.Succeeded;
            tx.LastError = null;
            _uow.Transactions.Update(tx);

            schedule.IsPaid = true;
            schedule.PaidAt = DateTime.UtcNow;
            schedule.TransactionId = tx.TransactionId;
            schedule.LastError = null;
            schedule.NextRetryAt = null;
            _uow.PaymentSchedules.Update(schedule);

            // Create remaining schedules NOW (only after signed+paid)
            if (plan.DurationMonths.HasValue && plan.IntervalMonths.HasValue && plan.StartDate.HasValue)
            {
                var durationMonths = plan.DurationMonths.Value;
                var intervalMonths = plan.IntervalMonths.Value;
                var paymentsCount = durationMonths / intervalMonths;

                var due = plan.StartDate.Value.Date.AddMonths(intervalMonths);
                for (int i = 2; i <= paymentsCount; i++)
                {
                    await _uow.PaymentSchedules.AddAsync(new PaymentSchedule
                    {
                        PaymentPlanId = plan.PaymentPlanId,
                        DueDate = due,
                        Amount = plan.PeriodicAmount,
                        IsPaid = false
                    });

                    due = due.AddMonths(intervalMonths);
                }
            }

            await _uow.CompleteAsync();
            return (true, "OK");
        }

        private async Task<(bool Success, string Message)> FinalizeRentStartFirstPaymentAsync(
            Transaction tx,
            PaymentCard payerCard,
            string cvv,
            decimal feePercent,
            long adminUserId)
        {
            // ✅ FIX: Amount is decimal
            if (tx.Amount <= 0) return (false, "Transaction amount invalid");
            if (tx.PaymentScheduleId == null) return (false, "Missing PaymentScheduleId");

            var schedule = await _uow.PaymentSchedules.GetByIdAsync(tx.PaymentScheduleId.Value);
            if (schedule == null) return (false, "PaymentSchedule not found");

            if (schedule.IsPaid) return (true, "Already paid");

            var plan = await _uow.PaymentPlans.GetByIdAsync(schedule.PaymentPlanId);
            if (plan == null) return (false, "PaymentPlan not found");

            if (plan.Status != PlanStatus.Active) return (false, "Plan not active");

            // Ensure plan is tied to same contract
            if (plan.ContractId == null || plan.ContractId != tx.ContractId)
                return (false, "Plan contract mismatch / missing");

            if (plan.StartDate == null || plan.EndDate == null)
                return (false, "Rent plan must have StartDate and EndDate");

            // Payee
            var landlordUserId = plan.PayeeUserId;

            var landlordCard = await _helpers.GetDefaultActiveCardAsync(landlordUserId);
            if (landlordCard == null) return (false, "Landlord has no active payment card");

            var adminCard = await _helpers.GetDefaultActiveCardAsync(adminUserId);
            if (adminCard == null) return (false, "Admin has no active payment card");

            // ✅ FIX: Amount is decimal
            var amount = tx.Amount;

            var fee = Math.Round(amount * feePercent / 100m, 2);
            var net = amount - fee;

            var payerToken = _enc.Decrypt(payerCard.CardTokenEncrypted);
            var payeeToken = _enc.Decrypt(landlordCard.CardTokenEncrypted);
            var adminToken = _enc.Decrypt(adminCard.CardTokenEncrypted);

            var bankRes = await _bank.TransferWithFeeAsync(payerToken, cvv, amount, payeeToken, adminToken, fee);

            tx.FeeAmount = fee;
            tx.NetToLandlord = net;
            tx.LandlordUserId = landlordUserId;
            tx.AdminUserId = adminUserId;

            if (!bankRes.Success)
            {
                tx.State = TransactionState.Failed;
                tx.LastError = bankRes.Message;
                _uow.Transactions.Update(tx);

                schedule.FailedAttempts += 1;
                schedule.LastFailureAt = DateTime.UtcNow;
                schedule.LastError = bankRes.Message;
                schedule.NextRetryAt = DateTime.UtcNow.AddHours(12);
                schedule.TransactionId = tx.TransactionId;
                _uow.PaymentSchedules.Update(schedule);

                await _uow.CompleteAsync();
                return (false, $"Payment failed: {bankRes.Message}");
            }

            // Success
            tx.State = TransactionState.Succeeded;
            tx.LastError = null;
            _uow.Transactions.Update(tx);

            schedule.IsPaid = true;
            schedule.PaidAt = DateTime.UtcNow;
            schedule.TransactionId = tx.TransactionId;
            schedule.LastError = null;
            schedule.NextRetryAt = null;
            _uow.PaymentSchedules.Update(schedule);

            // Create remaining rent schedules NOW (only after signed+paid)
            var start = plan.StartDate.Value.Date;
            var end = plan.EndDate.Value.Date;

            var d = start.AddMonths(1);
            while (d <= end)
            {
                await _uow.PaymentSchedules.AddAsync(new PaymentSchedule
                {
                    PaymentPlanId = plan.PaymentPlanId,
                    DueDate = d,
                    Amount = plan.PeriodicAmount,
                    IsPaid = false
                });

                d = d.AddMonths(1);
            }

            await _uow.CompleteAsync();
            return (true, "OK");
        }

        // =========================
        // Contract gate verification
        // =========================

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

            // integrity
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

        private AppDbContext2? TryGetDbContext()
        {
            return (_uow as UnitOfWork)?.Context;
        }
    }
}
