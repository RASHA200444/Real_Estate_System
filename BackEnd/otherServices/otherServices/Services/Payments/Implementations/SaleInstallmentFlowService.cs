using otherServices.Models;
using otherServices.Models.DTOs.Payments;
using otherServices.Models.Enums;
using otherServices.Repositories;
using otherServices.Services;
using otherServices.Services.Contracts;
using otherServices.Services.Payments.Flows;
using otherServices.Services.Payments.Helpers;

namespace otherServices.Services.Payments.Implementations
{
    public class SaleInstallmentFlowService : ISaleInstallmentFlowService
    {
        private readonly IUnitOfWork _uow;
        private readonly IEncryptionService _enc;
        private readonly IMockBankCardVault _bank;
        private readonly IContractService _contracts;
        private readonly IPaymentFlowHelpers _h;

        public SaleInstallmentFlowService(
            IUnitOfWork uow,
            IEncryptionService enc,
            IMockBankCardVault bank,
            IContractService contracts,
            IPaymentFlowHelpers helpers)
        {
            _uow = uow;
            _enc = enc;
            _bank = bank;
            _contracts = contracts;
            _h = helpers;
        }

        public async Task<object> ExecuteAsync(long userId, BuyPostRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ExternalRef))
                return new { success = false, message = "ExternalRef is required" };

            var existing = await _uow.Transactions.FirstOrDefaultAsync(t => t.ExternalRef == dto.ExternalRef);
            if (existing != null)
                return new { success = true, message = "Already processed", transactionId = existing.TransactionId };

            if (dto.ProposalId <= 0)
                return new { success = false, message = "ProposalId is required" };

            var post = await _uow.Posts.GetByIdAsync(dto.PostId);
            if (post == null) return new { success = false, message = "Post not found" };

            if (post.PendingStatus != PostPendingStatus.Accepted)
                return new { success = false, message = "Post is not approved by admin" };

            if (post.Type != PropertyType.Sale)
                return new { success = false, message = "BuyPost is for SALE only." };

            if (post.Status == PropertyStatus.Sold)
                return new { success = false, message = "Post is sold" };

            // ✅ must be in negotiation (proposal accepted)
            if (post.Status != PropertyStatus.UnderNegotiation)
                return new { success = false, message = "Post is not ready for installment payment" };

            var proposal = await _uow.Proposals.GetByIdAsync(dto.ProposalId);
            if (proposal == null) return new { success = false, message = "Proposal not found" };

            if (proposal.PostId != post.PostId)
                return new { success = false, message = "Proposal does not belong to this post" };

            if (proposal.TenantId != userId)
                return new { success = false, message = "You are not the owner of this proposal" };

            // ✅ must be approved before paying sale
            if (proposal.ProposalStatus != ProposalStatus.Approved)
                return new { success = false, message = "Proposal must be approved before payment" };

            var paymentCard = await _uow.PaymentCards.GetByIdAsync(dto.PaymentCardId);
            if (paymentCard == null || !paymentCard.IsActive)
                return new { success = false, message = "Payment card not found/active" };

            if (paymentCard.UserId != userId)
                return new { success = false, message = "You do not own this card" };

            if (string.IsNullOrWhiteSpace(dto.CVV))
                return new { success = false, message = "CVV is required" };

            if (!dto.InstallmentMonths.HasValue || dto.InstallmentMonths <= 0)
                return new { success = false, message = "InstallmentMonths is required" };

            if (!dto.Frequency.HasValue)
                return new { success = false, message = "Frequency is required" };

            var intervalMonths = (int)dto.Frequency.Value;
            if (intervalMonths <= 0) return new { success = false, message = "Invalid frequency" };

            var durationMonths = dto.InstallmentMonths.Value;
            if (durationMonths % intervalMonths != 0)
                return new { success = false, message = "InstallmentMonths must be divisible by frequency" };

            // ✅ determine total price (auction vs normal)
            decimal total;
            if (post.IsAuction)
            {
                if (!proposal.Offeredprice.HasValue || proposal.Offeredprice.Value <= 0)
                    return new { success = false, message = "Auction requires Offeredprice in proposal" };

                total = (decimal)proposal.Offeredprice.Value;

                // lock final price
                post.Price = proposal.Offeredprice.Value;
                _uow.Posts.Update(post);
            }
            else
            {
                if (proposal.Offeredprice.HasValue)
                    return new { success = false, message = "Offeredprice is not allowed for non-auction posts" };

                if (!post.Price.HasValue || post.Price.Value <= 0)
                    return new { success = false, message = "Post price is missing" };

                total = (decimal)post.Price.Value;
            }

            var paymentsCount = durationMonths / intervalMonths;
            if (paymentsCount <= 0) return new { success = false, message = "Invalid installment plan" };

            var perPayment = Math.Round(total / paymentsCount, 2);

            var landlord = await _uow.Landlords.GetByIdAsync(post.LandlordId);
            if (landlord == null) return new { success = false, message = "Seller not found" };

            var landlordUserId = landlord.UserId;

            // plan
            var plan = new PaymentPlan
            {
                PostId = post.PostId,
                PayerUserId = userId,
                PayeeUserId = landlordUserId,
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

            // first schedule
            var firstSchedule = new PaymentSchedule
            {
                PaymentPlanId = plan.PaymentPlanId,
                DueDate = DateTime.UtcNow.Date,
                Amount = perPayment,
                IsPaid = false
            };

            await _uow.PaymentSchedules.AddAsync(firstSchedule);
            await _uow.CompleteAsync();

            // tx first
            var tx = new Transaction
            {
                UserId = userId,
                PostId = post.PostId,
                ProposalId = proposal.ProposalId, // ✅ NEW
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

            await _uow.Transactions.AddAsync(tx);
            await _uow.CompleteAsync();

            try
            {
                tx.Attempts += 1;
                _uow.Transactions.Update(tx);
                await _uow.CompleteAsync();

                var adminUserId = _h.GetAdminUserId();

                var landlordCard = await _h.GetDefaultActiveCardAsync(landlordUserId);
                if (landlordCard == null)
                    return new { success = false, message = "Landlord has no active payment card" };

                var adminCard = await _h.GetDefaultActiveCardAsync(adminUserId);
                if (adminCard == null)
                    return new { success = false, message = "Admin has no active payment card" };

                var payerToken = _enc.Decrypt(paymentCard.CardTokenEncrypted);
                var payeeToken = _enc.Decrypt(landlordCard.CardTokenEncrypted);
                var adminToken = _enc.Decrypt(adminCard.CardTokenEncrypted);

                var feePercent = _h.GetFeePercent();
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

                // contract draft (after first payment)
                var snapshot = new
                {
                    ContractVersion = 1,
                    Type = ContractType.SaleInstallment,
                    PostId = post.PostId,
                    ProposalId = proposal.ProposalId,
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
                    proposalId: proposal.ProposalId,
                    type: ContractType.SaleInstallment,
                    snapshot: snapshot
                );

                var contract = await _uow.Contracts.GetByIdAsync(contractId);

                tx.ContractId = contractId;
                tx.ContractHash = contract?.ContractHash;

                tx.State = TransactionState.Succeeded;
                tx.LastError = null;
                _uow.Transactions.Update(tx);

                // mark first schedule paid
                firstSchedule.IsPaid = true;
                firstSchedule.PaidAt = DateTime.UtcNow;
                firstSchedule.TransactionId = tx.TransactionId;
                firstSchedule.LastError = null;
                firstSchedule.NextRetryAt = null;
                _uow.PaymentSchedules.Update(firstSchedule);

                // remaining schedules
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

                // keep under negotiation until fully paid
                post.Status = PropertyStatus.UnderNegotiation;
                _uow.Posts.Update(post);

                await _uow.CompleteAsync();

                return new
                {
                    success = true,
                    message = "Installment started + first payment paid + Contract Draft created",
                    paymentPlanId = plan.PaymentPlanId,
                    contractId = tx.ContractId,
                    contractHash = tx.ContractHash,
                    proposalId = proposal.ProposalId
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
    }
}
