using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using otherServices.Models;
using otherServices.Models.DTOs.Payments;
using otherServices.Models.Enums;
using otherServices.Repositories;
using otherServices.Services.Payments.Flows;

namespace otherServices.Services.Payments.Implementations
{
    public class SubscribeProFlowService : ISubscribeProFlowService
    {
        private readonly IUnitOfWork _uow;
        private readonly IEncryptionService _enc;
        private readonly IMockBankCardVault _bank;
        private readonly IConfiguration _cfg;

        public SubscribeProFlowService(
            IUnitOfWork uow,
            IEncryptionService enc,
            IMockBankCardVault bank,
            IConfiguration cfg)
        {
            _uow = uow;
            _enc = enc;
            _bank = bank;
            _cfg = cfg;
        }

        public async Task<object> ExecuteAsync(long landlordUserId, SubscribeProRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ExternalRef))
                return new { success = false, message = "ExternalRef is required" };

            // ✅ Idempotency
            var existing = await _uow.Transactions.FirstOrDefaultAsync(t => t.ExternalRef == dto.ExternalRef);
            if (existing != null)
                return new
                {
                    success = true,
                    message = "Already processed",
                    transactionId = existing.TransactionId,
                    state = existing.State
                };

            // ✅ landlord check
            var landlord = await _uow.Landlords.FirstOrDefaultAsync(l => l.UserId == landlordUserId);
            if (landlord == null)
                return new { success = false, message = "Landlord not found" };

            // ✅ plan
            var plan = await _uow.SubscriptionPlans.GetByIdAsync(dto.SubscriptionPlanId);
            if (plan == null || !plan.IsActive)
                return new { success = false, message = "Subscription plan not found/active" };

            // ✅ card
            var card = await _uow.PaymentCards.GetByIdAsync(dto.PaymentCardId);
            if (card == null || !card.IsActive)
                return new { success = false, message = "Payment card not found/active" };

            if (card.UserId != landlordUserId)
                return new { success = false, message = "You do not own this card" };

            if (string.IsNullOrWhiteSpace(dto.CVV))
                return new { success = false, message = "CVV is required" };

            var adminUserId = GetAdminUserId();
            var adminCard = await GetDefaultActiveCardAsync(adminUserId);
            if (adminCard == null)
                return new { success = false, message = "Admin has no active payment card" };

            // ✅ create tx first (safe)
            var tx = new Transaction
            {
                UserId = landlordUserId,
                PostId = null, // ✅ لازم PostId nullable في Transaction
                Amount = plan.Price,
                PaymentMethod = "Card",
                Status = TransactionStatus.purchased,
                State = TransactionState.Pending,
                Kind = TransactionKind.Subscription,
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

                var payerToken = _enc.Decrypt(card.CardTokenEncrypted);

                var adminToken = _enc.Decrypt(adminCard.CardTokenEncrypted);

                // ✅ subscription fee = 0, payee = admin
                var res = await _bank.TransferWithFeeAsync(
                    payerToken: payerToken,
                    cvv: dto.CVV,
                    amount: plan.Price,
                    payeeToken: adminToken,
                    adminToken: adminToken,
                    feeAmount: 0m
                );

                if (!res.Success)
                {
                    tx.State = TransactionState.Failed;
                    tx.LastError = res.Message;
                    _uow.Transactions.Update(tx);
                    await _uow.CompleteAsync();

                    return new { success = false, message = $"Payment failed: {res.Message}", transactionId = tx.TransactionId };
                }

                // ✅ create subscription
                var now = DateTime.UtcNow;
                var start = now;
                var end = now.AddMonths(plan.DurationInMonths);

                // لو عنده اشتراك active → ممكن تمده بدل ما تعمل واحد جديد
                // (هنا هنعمل تمديد بسيط)
                var activeSub = await _uow.UserSubscriptions.FirstOrDefaultAsync(s =>
                    s.UserId == landlordUserId && s.Status == SubscriptionStatus.Active && s.EndDate > now);

                if (activeSub != null)
                {
                    activeSub.EndDate = activeSub.EndDate.AddMonths(plan.DurationInMonths);
                    activeSub.SubscriptionPlanId = plan.SubscriptionPlanId;
                    activeSub.TransactionId = tx.TransactionId;
                    _uow.UserSubscriptions.Update(activeSub);
                }
                else
                {
                    var sub = new UserSubscription
                    {
                        UserId = landlordUserId,
                        SubscriptionPlanId = plan.SubscriptionPlanId,
                        StartDate = start,
                        EndDate = end,
                        Status = SubscriptionStatus.Active,
                        TransactionId = tx.TransactionId
                    };
                    await _uow.UserSubscriptions.AddAsync(sub);
                }

                // ✅ mark landlord pro
                landlord.IsPro = true;
                _uow.Landlords.Update(landlord);

                // ✅ finalize tx
                tx.State = TransactionState.Succeeded;
                tx.LastError = null;
                _uow.Transactions.Update(tx);

                await _uow.CompleteAsync();

                await CreateNotification(landlordUserId,
                    $"Subscription activated: {plan.Name} until {end:yyyy-MM-dd}");

                return new
                {
                    success = true,
                    message = "Subscription activated",
                    transactionId = tx.TransactionId,
                    planId = plan.SubscriptionPlanId,
                    endDate = end
                };
            }
            catch (Exception ex)
            {
                tx.State = TransactionState.Failed;
                tx.LastError = ex.InnerException?.Message ?? ex.Message;
                _uow.Transactions.Update(tx);
                await _uow.CompleteAsync();

                return new { success = false, message = "Payment error (safe). Retry with SAME ExternalRef.", transactionId = tx.TransactionId };
            }
        }

        private long GetAdminUserId()
        {
            var id = _cfg.GetValue<long>("Payments:AdminUserId");
            return id > 0 ? id : 4;
        }

        private async Task<PaymentCard?> GetDefaultActiveCardAsync(long userId)
        {
            var def = await _uow.PaymentCards.FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive && c.IsDefault);
            if (def != null) return def;

            return await _uow.PaymentCards.FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive);
        }

        private async Task CreateNotification(long userId, string content)
        {
            await _uow.Notifications.AddAsync(new Notification
            {
                UserId = userId,
                Content = content,
                ReadStatus = false,
                CreatedAt = DateTime.UtcNow
            });
            await _uow.CompleteAsync();
        }
    }
}
