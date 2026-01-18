using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using otherServices.Models;
using otherServices.Models.DTOs.Payments;
using otherServices.Models.Enums;
using otherServices.Repositories;
using otherServices.Services;
using otherServices.Services.Contracts;

namespace otherServices.Services.Payments
{
    public class PaymentFlowService : IPaymentFlowService
    {
        private readonly IUnitOfWork _uow;
        private readonly IEncryptionService _enc;
        private readonly IMockBankCardVault _bank;
        private readonly IConfiguration _cfg;
        private readonly IContractService _contracts;

        public PaymentFlowService(
            IUnitOfWork uow,
            IEncryptionService enc,
            IMockBankCardVault bank,
            IConfiguration cfg,
            IContractService contracts)
        {
            _uow = uow;
            _enc = enc;
            _bank = bank;
            _cfg = cfg;
            _contracts = contracts;
        }

        // =========================
        // 1) Buy Post (SALE only)
        // =========================
        public async Task<object> BuyPostAsync(long userId, BuyPostRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ExternalRef))
                return new { success = false, message = "ExternalRef is required" };

            var existing = await _uow.Transactions.FirstOrDefaultAsync(t => t.ExternalRef == dto.ExternalRef);
            if (existing != null)
                return new
                {
                    success = true,
                    message = "Already processed",
                    transactionId = existing.TransactionId,
                    status = existing.Status,
                    amount = existing.Amount,
                    state = existing.State
                };

            var user = await _uow.Users.GetByIdAsync(userId);
            if (user == null) return new { success = false, message = "User not found" };

            var post = await _uow.Posts.GetByIdAsync(dto.PostId);
            if (post == null) return new { success = false, message = "Post not found" };

            // ✅ Only admin-accepted posts can be purchased
            if (post.PendingStatus != PostPendingStatus.Accepted)
                return new { success = false, message = "Post is not approved by admin" };

            if (post.Type != PropertyType.Sale)
                return new { success = false, message = "BuyPost is for SALE only. Rent uses proposals." };

            if (post.Status != PropertyStatus.Available)
                return new { success = false, message = "Post is not available" };

            var paymentCard = await _uow.PaymentCards.GetByIdAsync(dto.PaymentCardId);
            if (paymentCard == null || !paymentCard.IsActive)
                return new { success = false, message = "Payment card not found/active" };
            if (paymentCard.UserId != userId)
                return new { success = false, message = "You do not own this card" };

            if (string.IsNullOrWhiteSpace(dto.CVV))
                return new { success = false, message = "CVV is required" };

            decimal total = (decimal)post.Price;

            // =========================
            // ✅ SALE CASH
            // =========================
            if (dto.IsInstallment == IsInstallment.Cash)
            {
                var tx = new Transaction
                {
                    UserId = userId,
                    PostId = post.PostId,
                    Amount = total,
                    PaymentMethod = "Card",
                    Status = TransactionStatus.purchased,
                    State = TransactionState.Pending,
                    Kind = TransactionKind.SaleCash,
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

                    var landlord = await _uow.Landlords.GetByIdAsync(post.LandlordId);
                    if (landlord == null)
                    {
                        tx.State = TransactionState.Failed;
                        tx.LastError = "Seller not found";
                        _uow.Transactions.Update(tx);
                        await _uow.CompleteAsync();
                        return new { success = false, message = "Seller not found", transactionId = tx.TransactionId };
                    }

                    var landlordUserId = landlord.UserId;
                    var adminUserId = GetAdminUserId(); // TEMP=4 or from config later

                    var landlordCard = await GetDefaultActiveCardAsync(landlordUserId);
                    if (landlordCard == null)
                    {
                        tx.State = TransactionState.Failed;
                        tx.LastError = "Landlord has no active payment card";
                        tx.LandlordUserId = landlordUserId;
                        tx.AdminUserId = adminUserId;
                        _uow.Transactions.Update(tx);
                        await _uow.CompleteAsync();
                        return new { success = false, message = "Landlord has no active payment card", transactionId = tx.TransactionId };
                    }

                    var adminCard = await GetDefaultActiveCardAsync(adminUserId);
                    if (adminCard == null)
                    {
                        tx.State = TransactionState.Failed;
                        tx.LastError = "Admin has no active payment card";
                        tx.LandlordUserId = landlordUserId;
                        tx.AdminUserId = adminUserId;
                        _uow.Transactions.Update(tx);
                        await _uow.CompleteAsync();
                        return new { success = false, message = "Admin has no active payment card", transactionId = tx.TransactionId };
                    }

                    var payerToken = _enc.Decrypt(paymentCard.CardTokenEncrypted);
                    var payeeToken = _enc.Decrypt(landlordCard.CardTokenEncrypted);
                    var adminToken = _enc.Decrypt(adminCard.CardTokenEncrypted);

                    var feePercent = GetFeePercent();
                    var fee = Math.Round(total * feePercent / 100m, 2);
                    var net = total - fee;

                    var res = await _bank.TransferWithFeeAsync(
                        payerToken: payerToken,
                        cvv: dto.CVV,
                        amount: total,
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
                        tx.State = TransactionState.Failed;
                        tx.LastError = res.Message;
                        _uow.Transactions.Update(tx);
                        await _uow.CompleteAsync();

                        return new { success = false, message = $"Payment failed: {res.Message}", transactionId = tx.TransactionId };
                    }

                    // ✅ Create contract draft snapshot (after successful payment)
                    var snapshot = new
                    {
                        ContractVersion = 1,
                        Type = ContractType.SaleCash,
                        PostId = post.PostId,
                        TenantId = userId,
                        LandlordUserId = landlordUserId,
                        Price = total,
                        FeeAmount = fee,
                        NetToLandlord = net,
                        CreatedAt = DateTime.UtcNow
                    };

                    var contractId = await _contracts.CreateDraftAsync(
                        postId: post.PostId,
                        tenantId: userId,
                        landlordUserId: landlordUserId,
                        proposalId: 0,
                        type: ContractType.SaleCash,
                        snapshot: snapshot
                    );

                    var contract = await _uow.Contracts.GetByIdAsync(contractId);

                    tx.ContractId = contractId;
                    tx.ContractHash = contract?.ContractHash;
                    tx.State = TransactionState.Succeeded;
                    tx.LastError = null;

                    _uow.Transactions.Update(tx);

                    post.Status = PropertyStatus.Sold;
                    _uow.Posts.Update(post);

                    await _uow.CompleteAsync();

                    await CreateNotification(userId, $"Purchase successful for '{post.Title}'. Contract created (Draft).");
                    await CreateNotification(landlordUserId, $"Your property '{post.Title}' was sold (cash). Contract created (Draft).");

                    return new
                    {
                        success = true,
                        message = "Purchased (cash) + Contract Draft created",
                        transactionId = tx.TransactionId,
                        contractId = tx.ContractId,
                        contractHash = tx.ContractHash
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

            // =========================
            // ✅ SALE INSTALLMENT
            // =========================
            if (!dto.InstallmentMonths.HasValue || dto.InstallmentMonths <= 0)
                return new { success = false, message = "InstallmentMonths is required" };

            if (!dto.Frequency.HasValue)
                return new { success = false, message = "Frequency is required" };

            var intervalMonths = (int)dto.Frequency.Value; // 1/3/6/12
            if (intervalMonths <= 0) return new { success = false, message = "Invalid frequency" };

            var durationMonths = dto.InstallmentMonths.Value;
            if (durationMonths % intervalMonths != 0)
                return new { success = false, message = "InstallmentMonths must be divisible by frequency" };

            var paymentsCount = durationMonths / intervalMonths;
            if (paymentsCount <= 0) return new { success = false, message = "Invalid installment plan" };

            var perPayment = Math.Round(total / paymentsCount, 2);

            var landlord2 = await _uow.Landlords.GetByIdAsync(post.LandlordId);
            if (landlord2 == null) return new { success = false, message = "Seller not found" };

            var plan = new PaymentPlan
            {
                PostId = post.PostId,
                PayerUserId = userId,
                PayeeUserId = landlord2.UserId,
                PropertyType = PropertyType.Sale,
                IsInstallment = IsInstallment.Installment,
                PaymentCardId = dto.PaymentCardId,
                StartDate = DateTime.UtcNow.Date,
                EndDate = null,
                DurationMonths = durationMonths,
                IntervalMonths = intervalMonths,
                TotalAmount = total,
                PeriodicAmount = perPayment,
                Status = PlanStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.PaymentPlans.AddAsync(plan);
            await _uow.CompleteAsync();

            var firstSchedule = new PaymentSchedule
            {
                PaymentPlanId = plan.PaymentPlanId,
                DueDate = DateTime.UtcNow.Date,
                Amount = perPayment,
                IsPaid = false
            };

            await _uow.PaymentSchedules.AddAsync(firstSchedule);
            await _uow.CompleteAsync();

            var txInstallment = new Transaction
            {
                UserId = userId,
                PostId = post.PostId,
                Amount = perPayment,
                PaymentMethod = "Card",
                Status = TransactionStatus.installment,
                State = TransactionState.Pending,
                Kind = TransactionKind.InstallmentPayment,
                ExternalRef = dto.ExternalRef,
                PaymentCardId = dto.PaymentCardId,
                Attempts = 0,
                CreatedAt = DateTime.UtcNow,
                PaymentScheduleId = firstSchedule.PaymentScheduleId
            };

            await _uow.Transactions.AddAsync(txInstallment);
            await _uow.CompleteAsync();

            try
            {
                txInstallment.Attempts += 1;
                _uow.Transactions.Update(txInstallment);
                await _uow.CompleteAsync();

                var adminUserId = GetAdminUserId();
                var landlordUserId = landlord2.UserId;

                var landlordCard = await GetDefaultActiveCardAsync(landlordUserId);
                if (landlordCard == null)
                    return new { success = false, message = "Landlord has no active payment card" };

                var adminCard = await GetDefaultActiveCardAsync(adminUserId);
                if (adminCard == null)
                    return new { success = false, message = "Admin has no active payment card" };

                var payerToken = _enc.Decrypt(paymentCard.CardTokenEncrypted);
                var payeeToken = _enc.Decrypt(landlordCard.CardTokenEncrypted);
                var adminToken = _enc.Decrypt(adminCard.CardTokenEncrypted);

                var feePercent = GetFeePercent();
                var fee = Math.Round(perPayment * feePercent / 100m, 2);
                var net = perPayment - fee;

                var res = await _bank.TransferWithFeeAsync(
                    payerToken: payerToken,
                    cvv: dto.CVV,
                    amount: perPayment,
                    payeeToken: payeeToken,
                    adminToken: adminToken,
                    feeAmount: fee
                );

                txInstallment.FeeAmount = fee;
                txInstallment.NetToLandlord = net;
                txInstallment.LandlordUserId = landlordUserId;
                txInstallment.AdminUserId = adminUserId;

                if (!res.Success)
                {
                    txInstallment.LastError = res.Message;
                    txInstallment.State = TransactionState.Failed;
                    _uow.Transactions.Update(txInstallment);

                    firstSchedule.FailedAttempts += 1;
                    firstSchedule.LastFailureAt = DateTime.UtcNow;
                    firstSchedule.LastError = res.Message;
                    firstSchedule.NextRetryAt = DateTime.UtcNow.AddHours(12);
                    _uow.PaymentSchedules.Update(firstSchedule);

                    await _uow.CompleteAsync();
                    return new { success = false, message = $"Payment failed: {res.Message}" };
                }

                // ✅ Create contract draft for installment (after first payment success)
                var snapshot = new
                {
                    ContractVersion = 1,
                    Type = ContractType.SaleInstallment,
                    PostId = post.PostId,
                    TenantId = userId,
                    LandlordUserId = landlordUserId,
                    TotalPrice = total,
                    DurationMonths = durationMonths,
                    IntervalMonths = intervalMonths,
                    FirstPaymentAmount = perPayment,
                    FeeAmount = fee,
                    NetToLandlord = net,
                    PaymentPlanId = plan.PaymentPlanId,
                    CreatedAt = DateTime.UtcNow
                };

                var contractId = await _contracts.CreateDraftAsync(
                    postId: post.PostId,
                    tenantId: userId,
                    landlordUserId: landlordUserId,
                    proposalId: 0,
                    type: ContractType.SaleInstallment,
                    snapshot: snapshot
                );

                var contract = await _uow.Contracts.GetByIdAsync(contractId);

                txInstallment.ContractId = contractId;
                txInstallment.ContractHash = contract?.ContractHash;

                txInstallment.State = TransactionState.Succeeded;
                txInstallment.LastError = null;
                _uow.Transactions.Update(txInstallment);

                firstSchedule.IsPaid = true;
                firstSchedule.PaidAt = DateTime.UtcNow;
                firstSchedule.TransactionId = txInstallment.TransactionId;
                firstSchedule.LastError = null;
                firstSchedule.NextRetryAt = null;
                _uow.PaymentSchedules.Update(firstSchedule);

                var due = DateTime.UtcNow.Date.AddMonths(intervalMonths);
                for (int i = 2; i <= paymentsCount; i++)
                {
                    await _uow.PaymentSchedules.AddAsync(new PaymentSchedule
                    {
                        PaymentPlanId = plan.PaymentPlanId,
                        DueDate = due,
                        Amount = perPayment,
                        IsPaid = false
                    });
                    due = due.AddMonths(intervalMonths);
                }

                post.Status = PropertyStatus.UnderNegotiation;
                _uow.Posts.Update(post);

                await _uow.CompleteAsync();

                return new
                {
                    success = true,
                    message = "Installment started + first payment paid + Contract Draft created",
                    paymentPlanId = plan.PaymentPlanId,
                    contractId = txInstallment.ContractId,
                    contractHash = txInstallment.ContractHash
                };
            }
            catch (Exception ex)
            {
                var err = ex.InnerException?.Message ?? ex.Message;

                txInstallment.LastError = err;
                txInstallment.State = TransactionState.Failed;
                _uow.Transactions.Update(txInstallment);

                firstSchedule.FailedAttempts += 1;
                firstSchedule.LastFailureAt = DateTime.UtcNow;
                firstSchedule.LastError = err;
                firstSchedule.NextRetryAt = DateTime.UtcNow.AddHours(12);
                _uow.PaymentSchedules.Update(firstSchedule);

                await _uow.CompleteAsync();
                return new { success = false, message = "Payment error (safe). Retry with SAME ExternalRef." };
            }
        }

        // ==================================
        // 2) Accept Proposal + Pay first rent month
        // ==================================
        public async Task<object> AcceptProposalAndStartAsync(long landlordUserId, AcceptProposalPayRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ExternalRef))
                return new { success = false, message = "ExternalRef is required" };

            var existing = await _uow.Transactions.FirstOrDefaultAsync(t => t.ExternalRef == dto.ExternalRef);
            if (existing != null)
                return new { success = true, message = "Already processed", transactionId = existing.TransactionId };

            var proposal = await _uow.Proposals.GetByIdAsync(dto.ProposalId);
            if (proposal == null) return new { success = false, message = "Proposal not found" };

            var post = await _uow.Posts.GetByIdAsync(proposal.PostId);
            if (post == null) return new { success = false, message = "Post not found" };

            // ✅ Constraints
            if (post.PendingStatus != PostPendingStatus.Accepted)
                return new { success = false, message = "Post is not approved by admin" };

            if (post.Status != PropertyStatus.Available)
                return new { success = false, message = "Post is not available" };

            if (post.Type != PropertyType.Rent)
                return new { success = false, message = "This flow is for RENT proposals only" };

            if (!proposal.StartRentalDate.HasValue || !proposal.EndRentalDate.HasValue)
                return new { success = false, message = "StartRentalDate and EndRentalDate are required for rent" };

            var start = proposal.StartRentalDate.Value.Date;
            var end = proposal.EndRentalDate.Value.Date;
            if (end <= start) return new { success = false, message = "EndRentalDate must be after StartRentalDate" };

            var landlord = await _uow.Landlords.GetByIdAsync(post.LandlordId);
            if (landlord == null) return new { success = false, message = "Landlord not found" };
            if (landlord.UserId != landlordUserId)
                return new { success = false, message = "You are not allowed to accept this proposal" };

            var tenantId = proposal.TenantId;

            var paymentCard = await _uow.PaymentCards.GetByIdAsync(dto.PaymentCardId);
            if (paymentCard == null || !paymentCard.IsActive) return new { success = false, message = "Payment card not found/active" };
            if (paymentCard.UserId != tenantId) return new { success = false, message = "Card does not belong to tenant" };

            if (string.IsNullOrWhiteSpace(dto.CVV))
                return new { success = false, message = "CVV is required" };

            decimal monthlyAmount = (decimal)post.Price;

            var plan = new PaymentPlan
            {
                PostId = post.PostId,
                PayerUserId = tenantId,
                PayeeUserId = landlordUserId,
                PropertyType = PropertyType.Rent,
                IsInstallment = IsInstallment.Installment,
                PaymentCardId = dto.PaymentCardId,
                StartDate = start,
                EndDate = end,
                TotalAmount = 0,
                PeriodicAmount = monthlyAmount,
                Status = PlanStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.PaymentPlans.AddAsync(plan);
            await _uow.CompleteAsync();

            var firstSchedule = new PaymentSchedule
            {
                PaymentPlanId = plan.PaymentPlanId,
                DueDate = start,
                Amount = monthlyAmount,
                IsPaid = false
            };
            await _uow.PaymentSchedules.AddAsync(firstSchedule);
            await _uow.CompleteAsync();

            var tx = new Transaction
            {
                UserId = tenantId,
                PostId = post.PostId,
                Amount = monthlyAmount,
                PaymentMethod = "Card",
                Status = TransactionStatus.installment,
                State = TransactionState.Pending,
                Kind = TransactionKind.Rent,
                ExternalRef = dto.ExternalRef,
                PaymentCardId = dto.PaymentCardId,
                Attempts = 0,
                CreatedAt = DateTime.UtcNow,
                PaymentScheduleId = firstSchedule.PaymentScheduleId
            };

            await _uow.Transactions.AddAsync(tx);
            await _uow.CompleteAsync();

            try
            {
                tx.Attempts += 1;
                _uow.Transactions.Update(tx);
                await _uow.CompleteAsync();

                var adminUserId = GetAdminUserId();

                var landlordCard = await GetDefaultActiveCardAsync(landlordUserId);
                if (landlordCard == null) return new { success = false, message = "Landlord has no active payment card" };

                var adminCard = await GetDefaultActiveCardAsync(adminUserId);
                if (adminCard == null) return new { success = false, message = "Admin has no active payment card" };

                var payerToken = _enc.Decrypt(paymentCard.CardTokenEncrypted);
                var payeeToken = _enc.Decrypt(landlordCard.CardTokenEncrypted);
                var adminToken = _enc.Decrypt(adminCard.CardTokenEncrypted);

                var feePercent = GetFeePercent();
                var fee = Math.Round(monthlyAmount * feePercent / 100m, 2);
                var net = monthlyAmount - fee;

                var res = await _bank.TransferWithFeeAsync(
                    payerToken: payerToken,
                    cvv: dto.CVV,
                    amount: monthlyAmount,
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

                    firstSchedule.FailedAttempts += 1;
                    firstSchedule.LastFailureAt = DateTime.UtcNow;
                    firstSchedule.LastError = res.Message;
                    firstSchedule.NextRetryAt = DateTime.UtcNow.AddHours(12);
                    _uow.PaymentSchedules.Update(firstSchedule);

                    await _uow.CompleteAsync();
                    return new { success = false, message = $"Payment failed: {res.Message}" };
                }

                // ✅ Create contract draft for rent
                var snapshot = new
                {
                    ContractVersion = 1,
                    Type = ContractType.Rent,
                    PostId = post.PostId,
                    ProposalId = proposal.ProposalId,
                    TenantId = tenantId,
                    LandlordUserId = landlordUserId,
                    StartDate = start,
                    EndDate = end,
                    MonthlyAmount = monthlyAmount,
                    FeeAmount = fee,
                    NetToLandlord = net,
                    PaymentPlanId = plan.PaymentPlanId,
                    CreatedAt = DateTime.UtcNow
                };

                var contractId = await _contracts.CreateDraftAsync(
                    postId: post.PostId,
                    tenantId: tenantId,
                    landlordUserId: landlordUserId,
                    proposalId: proposal.ProposalId,
                    type: ContractType.Rent,
                    snapshot: snapshot
                );

                var contract = await _uow.Contracts.GetByIdAsync(contractId);

                tx.ContractId = contractId;
                tx.ContractHash = contract?.ContractHash;

                tx.State = TransactionState.Succeeded;
                tx.LastError = null;
                _uow.Transactions.Update(tx);

                firstSchedule.IsPaid = true;
                firstSchedule.PaidAt = DateTime.UtcNow;
                firstSchedule.TransactionId = tx.TransactionId;
                firstSchedule.LastError = null;
                firstSchedule.NextRetryAt = null;
                _uow.PaymentSchedules.Update(firstSchedule);

                var d = start.AddMonths(1);
                while (d <= end)
                {
                    await _uow.PaymentSchedules.AddAsync(new PaymentSchedule
                    {
                        PaymentPlanId = plan.PaymentPlanId,
                        DueDate = d,
                        Amount = monthlyAmount,
                        IsPaid = false
                    });
                    d = d.AddMonths(1);
                }

                proposal.ProposalStatus = ProposalStatus.Approved;
                _uow.Proposals.Update(proposal);

                post.Status = PropertyStatus.UnderNegotiation;
                _uow.Posts.Update(post);

                await _uow.CompleteAsync();

                return new
                {
                    success = true,
                    message = "Proposal accepted + first month paid + schedule created + Contract Draft created",
                    paymentPlanId = plan.PaymentPlanId,
                    contractId = tx.ContractId,
                    contractHash = tx.ContractHash
                };
            }
            catch (Exception ex)
            {
                var err = ex.InnerException?.Message ?? ex.Message;

                tx.LastError = err;
                tx.State = TransactionState.Failed;
                _uow.Transactions.Update(tx);

                firstSchedule.FailedAttempts += 1;
                firstSchedule.LastFailureAt = DateTime.UtcNow;
                firstSchedule.LastError = err;
                firstSchedule.NextRetryAt = DateTime.UtcNow.AddHours(12);
                _uow.PaymentSchedules.Update(firstSchedule);

                await _uow.CompleteAsync();
                return new { success = false, message = "Payment error (safe). Retry with SAME ExternalRef." };
            }
        }

        // ==================================
        // 3) Pay remaining (manual amount)
        // ==================================
        public async Task<object> PayRemainingAsync(PayRemainingRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ExternalRef))
                return new { success = false, message = "ExternalRef is required" };

            if (dto.PaymentPlanId == null && dto.PaymentScheduleId == null)
                return new { success = false, message = "PaymentPlanId or PaymentScheduleId is required" };

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

            if (paymentCard.UserId != planOfFirst.PayerUserId)
                return new { success = false, message = "You do not own this card / payer mismatch" };

            decimal totalAmount = targets.Sum(x => x.Amount);

            var tx = new Transaction
            {
                UserId = planOfFirst.PayerUserId,
                PostId = planOfFirst.PostId,
                Amount = totalAmount,
                PaymentMethod = "Card",
                Status = TransactionStatus.installment,
                State = TransactionState.Pending,
                Kind = TransactionKind.InstallmentPayment,
                ExternalRef = dto.ExternalRef,
                PaymentCardId = dto.PaymentCardId,
                Attempts = 0,
                CreatedAt = DateTime.UtcNow,
                ContractId = null,
                ContractHash = null
            };

            await _uow.Transactions.AddAsync(tx);
            await _uow.CompleteAsync();

            try
            {
                tx.Attempts += 1;
                _uow.Transactions.Update(tx);
                await _uow.CompleteAsync();

                var adminUserId = GetAdminUserId();
                var landlordUserId = planOfFirst.PayeeUserId;

                var landlordCard = await GetDefaultActiveCardAsync(landlordUserId);
                if (landlordCard == null) return new { success = false, message = "Landlord has no active payment card" };

                var adminCard = await GetDefaultActiveCardAsync(adminUserId);
                if (adminCard == null) return new { success = false, message = "Admin has no active payment card" };

                var payerToken = _enc.Decrypt(paymentCard.CardTokenEncrypted);
                var payeeToken = _enc.Decrypt(landlordCard.CardTokenEncrypted);
                var adminToken = _enc.Decrypt(adminCard.CardTokenEncrypted);

                var feePercent = GetFeePercent();
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

                var remaining = await _uow.PaymentSchedules.FindAsync(s => s.PaymentPlanId == planOfFirst.PaymentPlanId && !s.IsPaid);

                if (!remaining.Any())
                {
                    planOfFirst.Status = PlanStatus.Completed;
                    _uow.PaymentPlans.Update(planOfFirst);

                    // sale installment completed -> Sold
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

                return new { success = true, message = "Paid", transactionId = tx.TransactionId, amount = totalAmount, fee, net };
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

        private decimal GetFeePercent()
        {
            var p = _cfg.GetValue<decimal>("Payments:PlatformFeePercent");
            return p <= 0 ? 0 : p;
        }

        private long GetAdminUserId()
        {
            // ✅ TEMP: admin userId = 4 (you can switch to config later)
            var id = _cfg.GetValue<long>("Payments:AdminUserId");
            return id > 0 ? id : 4;
        }

        private async Task<PaymentCard?> GetDefaultActiveCardAsync(long userId)
        {
            var def = await _uow.PaymentCards.FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive && c.IsDefault);
            if (def != null) return def;
            return await _uow.PaymentCards.FirstOrDefaultAsync(c => c.UserId == userId && c.IsActive);
        }
    }
}
