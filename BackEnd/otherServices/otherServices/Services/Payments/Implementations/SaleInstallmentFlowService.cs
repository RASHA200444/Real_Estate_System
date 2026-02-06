using otherServices.Models;
using otherServices.Models.DTOs.Payments;
using otherServices.Models.Enums;
using otherServices.Repositories;
using otherServices.Services.Contracts;
using otherServices.Services.Payments.Flows;
using otherServices.Services.Payments.Helpers;

namespace otherServices.Services.Payments.Implementations
{
    public class SaleInstallmentFlowService : ISaleInstallmentFlowService
    {
        private readonly IUnitOfWork _uow;
        private readonly IContractService _contracts;
        private readonly IPaymentFlowHelpers _h;

        public SaleInstallmentFlowService(
            IUnitOfWork uow,
            IContractService contracts,
            IPaymentFlowHelpers helpers)
        {
            _uow = uow;
            _contracts = contracts;
            _h = helpers;
        }

        // ✅ NOW: Initiate ONLY (Create Plan + First Schedule + Contract Draft + Tx AwaitingSignatures)
        public async Task<object> ExecuteAsync(long userId, SaleInstallmentRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ExternalRef))
                return new { success = false, message = "ExternalRef is required" };

            var existing = await _uow.Transactions.FirstOrDefaultAsync(t => t.ExternalRef == dto.ExternalRef);
            if (existing != null)
            {
                return new
                {
                    success = true,
                    message = "Already initiated",
                    transactionId = existing.TransactionId,
                    contractId = existing.ContractId,
                    contractHash = existing.ContractHash,
                    state = existing.State.ToString(),
                    nextAction = existing.State == TransactionState.Succeeded ? "NONE" : "SIGN_AND_FINALIZE"
                };
            }

            if (dto.ProposalId <= 0)
                return new { success = false, message = "ProposalId is required" };

            var post = await _uow.Posts.GetByIdAsync(dto.PostId);
            if (post == null) return new { success = false, message = "Post not found" };

            if (post.PendingStatus != PostPendingStatus.Accepted)
                return new { success = false, message = "Post is not approved by admin" };

            if (post.Type != PropertyType.Sale)
                return new { success = false, message = "SaleInstallment is for SALE only." };

            if (post.Status == PropertyStatus.Sold)
                return new { success = false, message = "Post is sold" };

            if (post.Status != PropertyStatus.UnderNegotiation)
                return new { success = false, message = "Post is not ready for installment initiation" };

            var proposal = await _uow.Proposals.GetByIdAsync(dto.ProposalId);
            if (proposal == null) return new { success = false, message = "Proposal not found" };

            if (proposal.PostId != post.PostId)
                return new { success = false, message = "Proposal does not belong to this post" };

            if (proposal.TenantId != userId)
                return new { success = false, message = "You are not the owner of this proposal" };

            if (proposal.ProposalStatus != ProposalStatus.Approved)
                return new { success = false, message = "Proposal must be approved before initiating installment" };

            // ✅ GATE: Eligibility required for installment
            if (proposal.IsAble == AIInstallmentDecision.Disable)
            {
                return new
                {
                    success = false,
                    message = "Eligibility check required before installment payment. Submit eligibility form first.",
                    installmentDecision = (int)proposal.IsAble,
                    eligibilityScore = proposal.EligibilityScore,
                    eligibilityReason = proposal.EligibilityReason
                };
            }

            var paymentCard = await _uow.PaymentCards.GetByIdAsync(dto.PaymentCardId);
            if (paymentCard == null || !paymentCard.IsActive)
                return new { success = false, message = "Payment card not found/active" };

            if (paymentCard.UserId != userId)
                return new { success = false, message = "You do not own this card" };

            if (dto.InstallmentMonths <= 0)
                return new { success = false, message = "InstallmentMonths is required" };

            if (dto.Frequency <= 0)
                return new { success = false, message = "Frequency is required" };

            var intervalMonths = dto.Frequency;
            var durationMonths = dto.InstallmentMonths;

            if (durationMonths % intervalMonths != 0)
                return new { success = false, message = "InstallmentMonths must be divisible by frequency" };

            decimal total;

            if (post.IsAuction)
            {
                if (!proposal.Offeredprice.HasValue || proposal.Offeredprice.Value <= 0)
                    return new { success = false, message = "Auction requires Offeredprice in proposal" };

                total = (decimal)proposal.Offeredprice.Value;

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

            // Create plan (no bank movement)
            var feePercent = _h.GetFeePercent();

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
                PlatformFeePercent = feePercent,
                Status = PlanStatus.Active,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.PaymentPlans.AddAsync(plan);
            await _uow.CompleteAsync();

            // First schedule only (unpaid now)
            var firstSchedule = new PaymentSchedule
            {
                PaymentPlanId = plan.PaymentPlanId,
                DueDate = DateTime.UtcNow.Date,
                Amount = perPayment,
                IsPaid = false
            };

            await _uow.PaymentSchedules.AddAsync(firstSchedule);
            await _uow.CompleteAsync();

            // Contract draft (terms)
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
                PeriodicAmount = perPayment,
                PaymentPlanId = plan.PaymentPlanId,
                PlatformFeePercent = feePercent,
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

            // Link plan to contract (for PayRemaining gate)
            plan.ContractId = contractId;
            plan.ContractHash = contract?.ContractHash;
            _uow.PaymentPlans.Update(plan);
            await _uow.CompleteAsync();

            // Create transaction (no transfer yet)
            var tx = new Transaction
            {
                UserId = userId,
                PostId = post.PostId,
                ProposalId = proposal.ProposalId,
                Amount = perPayment,
                PaymentMethod = "Card",
                Status = TransactionStatus.installment,
                State = TransactionState.AwaitingSignatures,
                Kind = TransactionKind.SaleInstallment,
                ExternalRef = dto.ExternalRef,
                PaymentCardId = dto.PaymentCardId,
                Attempts = 0,
                CreatedAt = DateTime.UtcNow,
                PaymentScheduleId = firstSchedule.PaymentScheduleId,
                ContractId = contractId,
                ContractHash = contract?.ContractHash
            };

            await _uow.Transactions.AddAsync(tx);
            await _uow.CompleteAsync();

            return new
            {
                success = true,
                message = "Installment initiated. Contract draft created. Sign then finalize to pay first installment.",
                paymentPlanId = plan.PaymentPlanId,
                firstScheduleId = firstSchedule.PaymentScheduleId,
                transactionId = tx.TransactionId,
                externalRef = tx.ExternalRef,
                contractId,
                contractHash = tx.ContractHash,
                nextAction = "SIGN_CONTRACT_THEN_FINALIZE"
            };
        }
    }
}
