using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using otherServices.Repositories;
using otherServices.Services;
using otherServices.Models;
using otherServices.Models.Enums;

namespace otherServices.Services.Payments
{
    public class RecurringPaymentsWorker : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _config;

        public RecurringPaymentsWorker(IServiceScopeFactory scopeFactory, IConfiguration config)
        {
            _scopeFactory = scopeFactory;
            _config = config;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var intervalMin = _config.GetValue<int>("Payments:WorkerIntervalMinutes", 60);
            var maxAttempts = _config.GetValue<int>("Payments:MaxAttemptsPerTransaction", 3);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessDueSchedules(maxAttempts, stoppingToken);
                }
                catch
                {
                    // don't crash the service
                }

                await Task.Delay(TimeSpan.FromMinutes(intervalMin), stoppingToken);
            }
        }

        private async Task ProcessDueSchedules(int maxAttempts, CancellationToken token)
        {
            using var scope = _scopeFactory.CreateScope();
            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var enc = scope.ServiceProvider.GetRequiredService<IEncryptionService>();
            var bank = scope.ServiceProvider.GetRequiredService<IMockBankCardVault>();

            var now = DateTime.UtcNow;
            var today = now.Date;

            // due <= today && not paid && attempts < 3 && (NextRetryAt null أو <= now)
            var due = await uow.PaymentSchedules.FindAsync(s =>
                !s.IsPaid &&
                s.DueDate <= today &&
                s.FailedAttempts < maxAttempts &&
                (s.NextRetryAt == null || s.NextRetryAt <= now));

            foreach (var item in due.OrderBy(x => x.DueDate))
            {
                if (token.IsCancellationRequested) break;

                var plan = await uow.PaymentPlans.GetByIdAsync(item.PaymentPlanId);
                if (plan == null || plan.Status != PlanStatus.Active)
                    continue;

                var card = await uow.PaymentCards.GetByIdAsync(plan.PaymentCardId);
                if (card == null || !card.IsActive)
                {
                    await FailAndMaybeEscalate(uow, plan, item, "Payment card missing/disabled", maxAttempts);
                    continue;
                }

                // Idempotency ExternalRef لكل schedule
                var externalRef = $"PLAN:{item.PaymentScheduleId}:{item.DueDate:yyyyMMdd}";

                // لو عندنا tx بنفس ExternalRef يبقى العملية اتسجلت قبل كده
                var oldTx = await uow.Transactions.FirstOrDefaultAsync(t => t.ExternalRef == externalRef);
                if (oldTx != null && item.IsPaid)
                    continue;

                // create tx first (safe)
                var tx = new Transaction
                {
                    UserId = plan.PayerUserId,
                    PostId = plan.PostId,
                    Amount = item.Amount,
                    PaymentMethod = "Card",

                    Status = TransactionStatus.installment,
                    State = TransactionState.Pending,
                    Kind = TransactionKind.InstallmentPayment,

                    ExternalRef = externalRef,
                    PaymentCardId = plan.PaymentCardId,
                    Attempts = 0,
                    CreatedAt = now,
                    PaymentScheduleId = item.PaymentScheduleId
                };


                await uow.Transactions.AddAsync(tx);
                await uow.CompleteAsync();

                try
                {
                    tx.Attempts += 1;
                    uow.Transactions.Update(tx);
                    await uow.CompleteAsync();

                    // payer token
                    var payerToken = enc.Decrypt(card.CardTokenEncrypted);

                    // payee token (landlord) + admin token
                    var landlordCard = await GetDefaultActiveCardAsync(uow, plan.PayeeUserId);
                    if (landlordCard == null)
                    {
                        await FailAndMaybeEscalate(uow, plan, item, "Landlord has no active payment card", maxAttempts);
                        continue;
                    }

                    var adminUserId = GetAdminUserId();
                    var adminCard = await GetDefaultActiveCardAsync(uow, adminUserId);
                    if (adminCard == null)
                    {
                        await FailAndMaybeEscalate(uow, plan, item, "Admin has no active payment card", maxAttempts);
                        continue;
                    }

                    var payeeToken = enc.Decrypt(landlordCard.CardTokenEncrypted);
                    var adminToken = enc.Decrypt(adminCard.CardTokenEncrypted);

                    // fee + net
                    var fee = CalcFee(item.Amount);
                    var net = item.Amount - fee;

                    // ✅ recurring بدون CVV -> "000"
                    var transfer = await bank.TransferWithFeeAsync(
                        payerToken: payerToken,
                        cvv: "000",
                        amount: item.Amount,
                        payeeToken: payeeToken,
                        adminToken: adminToken,
                        feeAmount: fee
                    );

                    if (!transfer.Success)
                    {
                        tx.LastError = transfer.Message;
                        tx.State = TransactionState.Failed;     // ✅ IMPORTANT
                        tx.FeeAmount = fee;
                        tx.NetToLandlord = net;
                        tx.LandlordUserId = plan.PayeeUserId;
                        tx.AdminUserId = adminUserId;
                        uow.Transactions.Update(tx);

                        item.FailedAttempts += 1;
                        item.LastFailureAt = DateTime.UtcNow;
                        item.LastError = transfer.Message;
                        item.NextRetryAt = DateTime.UtcNow.AddHours(12);
                        uow.PaymentSchedules.Update(item);

                        await uow.CompleteAsync();

                        await Notify(uow, plan.PayerUserId,
                            $"Auto payment failed (Due {item.DueDate:yyyy-MM-dd}): {transfer.Message}");

                        if (item.FailedAttempts >= maxAttempts)
                            await EscalateToAdmins(uow, plan, item, transfer.Message);

                        continue;
                    }

                    // ✅ success
                    tx.State = TransactionState.Succeeded;     // ✅ IMPORTANT
                    tx.LastError = null;
                    tx.FeeAmount = fee;
                    tx.NetToLandlord = net;
                    tx.LandlordUserId = plan.PayeeUserId;
                    tx.AdminUserId = adminUserId;
                    uow.Transactions.Update(tx);
                    await uow.CompleteAsync();


                    // success
                    item.IsPaid = true;
                    item.PaidAt = DateTime.UtcNow;
                    item.TransactionId = tx.TransactionId;
                    item.LastError = null;
                    item.NextRetryAt = null;
                    uow.PaymentSchedules.Update(item);

                    await uow.CompleteAsync();

                    await Notify(uow, plan.PayerUserId,
                        $"Auto payment success for Due {item.DueDate:yyyy-MM-dd}.");

                    await Notify(uow, plan.PayeeUserId,
                        $"Payment received for Due {item.DueDate:yyyy-MM-dd}.");

                    // لو كل الدفعات اتدفعت -> Completed
                    var remaining = await uow.PaymentSchedules.FindAsync(s =>
                        s.PaymentPlanId == plan.PaymentPlanId && !s.IsPaid);
                    if (!remaining.Any())
                    {
                        plan.Status = PlanStatus.Completed;
                        uow.PaymentPlans.Update(plan);
                        await uow.CompleteAsync();
                    }
                }
                catch (Exception ex)
                {
                    var err = ex.InnerException?.Message ?? ex.Message;

                    tx.LastError = err;
                    uow.Transactions.Update(tx);

                    item.FailedAttempts += 1;
                    item.LastFailureAt = DateTime.UtcNow;
                    item.LastError = err;
                    item.NextRetryAt = DateTime.UtcNow.AddHours(12);
                    uow.PaymentSchedules.Update(item);

                    await uow.CompleteAsync();

                    if (item.FailedAttempts >= maxAttempts)
                        await EscalateToAdmins(uow, plan, item, err);
                }
            }
        }

        private async Task<PaymentCard?> GetDefaultActiveCardAsync(IUnitOfWork uow, long userId)
        {
            var def = await uow.PaymentCards.FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive && c.IsDefault);
            if (def != null) return def;

            return await uow.PaymentCards.FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive);
        }

        private decimal CalcFee(decimal amount)
        {
            var percent = _config.GetValue<decimal>("Payments:PlatformFeePercent", 0);
            if (percent <= 0) return 0m;
            return Math.Round(amount * percent / 100m, 2);
        }

        private long GetAdminUserId()
        {
            var adminUserId = _config.GetValue<long>("Payments:AdminUserId", 0);
            if (adminUserId <= 0) throw new Exception("Payments:AdminUserId is not configured");
            return adminUserId;
        }


        private async Task FailAndMaybeEscalate(IUnitOfWork uow, PaymentPlan plan, PaymentSchedule item, string err, int maxAttempts)
        {
            item.FailedAttempts += 1;
            item.LastFailureAt = DateTime.UtcNow;
            item.LastError = err;
            item.NextRetryAt = DateTime.UtcNow.AddHours(12);
            uow.PaymentSchedules.Update(item);
            await uow.CompleteAsync();

            await Notify(uow, plan.PayerUserId, $"Auto payment failed: {err}");

            if (item.FailedAttempts >= maxAttempts)
                await EscalateToAdmins(uow, plan, item, err);
        }

        private async Task Notify(IUnitOfWork uow, long userId, string content)
        {
            await uow.Notifications.AddAsync(new Notification
            {
                UserId = userId,
                Content = content,
                ReadStatus = false,
                CreatedAt = DateTime.UtcNow
            });
            await uow.CompleteAsync();
        }

        private async Task EscalateToAdmins(IUnitOfWork uow, PaymentPlan plan, PaymentSchedule item, string err)
        {
            if (item.EscalatedToAdmin) return;

            item.EscalatedToAdmin = true;
            uow.PaymentSchedules.Update(item);
            await uow.CompleteAsync();

            var admins = await uow.Admins.GetAllAsync();
            foreach (var a in admins)
            {
                await Notify(uow, a.UserId,
                    $"ALERT: Payment failed 3 times. PlanId={plan.PaymentPlanId}, Due={item.DueDate:yyyy-MM-dd}, Error={err}");
            }
        }
    }
}
