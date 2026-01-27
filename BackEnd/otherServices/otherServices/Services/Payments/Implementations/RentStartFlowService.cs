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
    public class RentStartFlowService : IRentStartFlowService
    {
        private readonly IUnitOfWork _uow;
        private readonly IEncryptionService _enc;
        private readonly IMockBankCardVault _bank;
        private readonly IContractService _contracts;
        private readonly IPaymentFlowHelpers _h;

        public RentStartFlowService(
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

        public async Task<object> ExecuteAsync(long landlordUserId, RentStartPaymentRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ExternalRef))
                return new { success = false, message = "ExternalRef is required" };

            // idempotency
            var existing = await _uow.Transactions.FirstOrDefaultAsync(t => t.ExternalRef == dto.ExternalRef);
            if (existing != null)
                return new { success = true, message = "Already processed", transactionId = existing.TransactionId };

            var proposal = await _uow.Proposals.GetByIdAsync(dto.ProposalId);
            if (proposal == null) return new { success = false, message = "Proposal not found" };

            // ✅ Option B: landlord MUST accept proposal first
            if (proposal.ProposalStatus != ProposalStatus.Approved)
                return new { success = false, message = "Proposal must be Approved before starting rent payment" };

            var post = await _uow.Posts.GetByIdAsync(proposal.PostId);
            if (post == null) return new { success = false, message = "Post not found" };

            if (post.PendingStatus != PostPendingStatus.Accepted)
                return new { success = false, message = "Post is not approved by admin" };

            if (post.Status == PropertyStatus.Sold)
                return new { success = false, message = "Post is sold" };

            // ✅ After AcceptProposal, it should be UnderNegotiation
            if (post.Status != PropertyStatus.UnderNegotiation)
                return new { success = false, message = "Post must be UnderNegotiation after proposal acceptance" };

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
                return new { success = false, message = "You are not allowed to start rent for this post" };

            // ✅ Ensure there isn't another approved proposal (other than this one)
            var alreadyApproved = await _uow.Proposals.FirstOrDefaultAsync(p =>
                p.PostId == post.PostId && p.ProposalStatus == ProposalStatus.Approved);

            if (alreadyApproved != null && alreadyApproved.ProposalId != proposal.ProposalId)
                return new { success = false, message = "Another proposal is already approved for this post" };

            // ✅ Prevent double-start: if there's already an active rent plan for this tenant+post
            var existingPlan = await _uow.PaymentPlans.FirstOrDefaultAsync(pp =>
                pp.PostId == post.PostId &&
                pp.PayerUserId == proposal.TenantId &&
                pp.PropertyType == PropertyType.Rent &&
                pp.Status == PlanStatus.Active);

            if (existingPlan != null)
                return new { success = false, message = "Rent plan already started for this proposal/post", paymentPlanId = existingPlan.PaymentPlanId };

            // ✅ Optional: prevent double-start if rent tx already succeeded for this proposal
            var existingRentTx = await _uow.Transactions.FirstOrDefaultAsync(t =>
                t.ProposalId == proposal.ProposalId &&
                t.Kind == TransactionKind.Rent &&
                t.State == TransactionState.Succeeded);

            if (existingRentTx != null)
                return new { success = false, message = "Rent already started/paid for this proposal", transactionId = existingRentTx.TransactionId };

            // ✅ GATE: Eligibility required for rent (only if you already added these fields)
            // لو الحقول دي مش موجودة عندك دلوقتي شيل البلوك ده
            //if (proposal.RentIsAble != AIRentDecision.Able)

            // ✅ TEMP: allow NotCertain until AI is implemented.
            // Block only if rent eligibility is explicitly Disabled.
            if (proposal.RentIsAble == AIRentDecision.Disable)

            {
                return new
                {
                    success = false,
                    message = "Eligibility check required before rent start. Tenant must submit eligibility form first.",
                    rentDecision = (int)proposal.RentIsAble,
                    eligibilityScore = proposal.EligibilityScore,
                    eligibilityReason = proposal.EligibilityReason
                };
            }

            var tenantId = proposal.TenantId;

            var paymentCard = await _uow.PaymentCards.GetByIdAsync(dto.PaymentCardId);
            if (paymentCard == null || !paymentCard.IsActive)
                return new { success = false, message = "Payment card not found/active" };

            if (paymentCard.UserId != tenantId)
                return new { success = false, message = "Card does not belong to tenant" };

            if (string.IsNullOrWhiteSpace(dto.CVV))
                return new { success = false, message = "CVV is required" };

            // ✅ monthly amount rules (auction vs normal)
            decimal monthlyAmount;

            if (post.IsAuction)
            {
                if (!proposal.Offeredprice.HasValue || proposal.Offeredprice.Value <= 0)
                    return new { success = false, message = "Auction requires Offeredprice in proposal" };

                monthlyAmount = (decimal)proposal.Offeredprice.Value;

                post.Price = proposal.Offeredprice.Value;
                _uow.Posts.Update(post);
            }
            else
            {
                if (proposal.Offeredprice.HasValue)
                    return new { success = false, message = "Offeredprice not allowed for non-auction rent" };

                if (!post.Price.HasValue || post.Price.Value <= 0)
                    return new { success = false, message = "Post price is missing" };

                monthlyAmount = (decimal)post.Price.Value;
            }

            var plan = new PaymentPlan
            {
                PostId = post.PostId,
                PayerUserId = tenantId,
                PayeeUserId = landlordUserId,
                PropertyType = PropertyType.Rent,
                IsInstallment = IsInstallment.Installment, // rent = recurring
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
                ProposalId = proposal.ProposalId,
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

                var adminUserId = _h.GetAdminUserId();

                var landlordCard = await _h.GetDefaultActiveCardAsync(landlordUserId);
                if (landlordCard == null) return new { success = false, message = "Landlord has no active payment card" };

                var adminCard = await _h.GetDefaultActiveCardAsync(adminUserId);
                if (adminCard == null) return new { success = false, message = "Admin has no active payment card" };

                var payerToken = _enc.Decrypt(paymentCard.CardTokenEncrypted);
                var payeeToken = _enc.Decrypt(landlordCard.CardTokenEncrypted);
                var adminToken = _enc.Decrypt(adminCard.CardTokenEncrypted);

                var feePercent = _h.GetFeePercent();
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

                // schedule remaining months
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

                // proposal already Approved from AcceptProposal => keep it as Approved (no need to update/reject others here)
                // post already UnderNegotiation from AcceptProposal => keep it

                await _uow.CompleteAsync();

                return new
                {
                    success = true,
                    message = "Rent started + first month paid + schedule created + Contract Draft created",
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
