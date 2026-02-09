using Microsoft.EntityFrameworkCore;
using otherServices.Infrastructure.Kafka;
using otherServices.Infrastructure.Kafka.Models;
using otherServices.Models;
using otherServices.Models.DTOs.Payments;
using otherServices.Models.Enums;
using otherServices.Repositories;
using otherServices.Services.Contracts;
using otherServices.Services.Payments.Flows;
using otherServices.Services.Payments.Helpers;

namespace otherServices.Services.Payments.Implementations
{
    public class RentStartFlowService : IRentStartFlowService
    {
        private const int AI_COOLDOWN_MINUTES = 5;

        private readonly IUnitOfWork _uow;
        private readonly IContractService _contracts;
        private readonly IPaymentFlowHelpers _h;

        private readonly AppDbContext2 _context;
        private readonly IAiRequestDispatcher _ai;

        public RentStartFlowService(
            IUnitOfWork uow,
            IContractService contracts,
            IPaymentFlowHelpers helpers,
            AppDbContext2 context,
            IAiRequestDispatcher aiRequestDispatcher)
        {
            _uow = uow;
            _contracts = contracts;
            _h = helpers;

            _context = context;
            _ai = aiRequestDispatcher;
        }

        private async Task<bool> WasSentRecentlyAsync(string requestType, string entityType, long entityId)
        {
            var since = DateTime.UtcNow.AddMinutes(-AI_COOLDOWN_MINUTES);

            return await _context.Set<AiOutboxMessage>()
                .AsNoTracking()
                .AnyAsync(x =>
                    x.RequestType == requestType &&
                    x.EntityType == entityType &&
                    x.EntityId == entityId &&
                    x.CreatedAtUtc >= since
                );
        }

        private async Task<object?> GuardIdentityForPaymentAsync(long tenantUserId)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == tenantUserId);
            if (user == null)
                return new { success = false, message = "User not found" };

            if (user.NIDEvaluation == AIDecision.Fraudulent)
                return new { success = false, message = "Identity verification failed (Fraudulent NID). Payment is blocked." };

            if (user.NIDEvaluation == AIDecision.NotReviewed || user.NIDEvaluation == AIDecision.Uncertain)
            {
                var sent = await WasSentRecentlyAsync(AiRequestTypes.Fraud_DocumentAnalysis, "user", user.UserId);
                if (!sent && !string.IsNullOrWhiteSpace(user.NIDPath))
                {
                    await _ai.EnqueueAsync(
                        requestType: AiRequestTypes.Fraud_DocumentAnalysis,
                        entityType: "user",
                        entityId: user.UserId,
                        payload: new { nidPath = user.NIDPath }
                    );
                    await _context.SaveChangesAsync();
                }

                return new { success = false, message = "Identity verification is pending. Please try again later." };
            }

            return null; // ok
        }

        public async Task<object> ExecuteAsync(long tenantUserId, RentStartPaymentRequestDto dto)
        {
            // ✅ NEW: identity gate
            var guard = await GuardIdentityForPaymentAsync(tenantUserId);
            if (guard != null) return guard;

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

            var proposal = await _uow.Proposals.GetByIdAsync(dto.ProposalId);
            if (proposal == null) return new { success = false, message = "Proposal not found" };

            if (proposal.TenantId != tenantUserId)
                return new { success = false, message = "Only the tenant can start rent for this proposal" };

            if (proposal.ProposalStatus != ProposalStatus.Approved)
                return new { success = false, message = "Proposal must be Approved before starting rent flow" };

            var post = await _uow.Posts.GetByIdAsync(proposal.PostId);
            if (post == null) return new { success = false, message = "Post not found" };

            if (post.PendingStatus != PostPendingStatus.Accepted)
                return new { success = false, message = "Post is not approved by admin" };

            if (post.Status == PropertyStatus.Sold)
                return new { success = false, message = "Post is sold" };

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

            var landlordUserId = landlord.UserId;

            var alreadyApproved = await _uow.Proposals.FirstOrDefaultAsync(p =>
                p.PostId == post.PostId && p.ProposalStatus == ProposalStatus.Approved);

            if (alreadyApproved != null && alreadyApproved.ProposalId != proposal.ProposalId)
                return new { success = false, message = "Another proposal is already approved for this post" };

            var existingPlan = await _uow.PaymentPlans.FirstOrDefaultAsync(pp =>
                pp.PostId == post.PostId &&
                pp.PayerUserId == tenantUserId &&
                pp.PropertyType == PropertyType.Rent &&
                pp.Status == PlanStatus.Active);

            if (existingPlan != null)
                return new
                {
                    success = false,
                    message = "Rent plan already started for this proposal/post",
                    paymentPlanId = existingPlan.PaymentPlanId
                };

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

            var paymentCard = await _uow.PaymentCards.GetByIdAsync(dto.PaymentCardId);
            if (paymentCard == null || !paymentCard.IsActive)
                return new { success = false, message = "Payment card not found/active" };

            if (paymentCard.UserId != tenantUserId)
                return new { success = false, message = "Card does not belong to tenant" };

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

            var feePercent = _h.GetFeePercent();

            var plan = new PaymentPlan
            {
                PostId = post.PostId,
                PayerUserId = tenantUserId,
                PayeeUserId = landlordUserId,
                PropertyType = PropertyType.Rent,
                IsInstallment = IsInstallment.Installment,
                PaymentCardId = dto.PaymentCardId,
                StartDate = start,
                EndDate = end,
                TotalAmount = 0,
                PeriodicAmount = monthlyAmount,
                PlatformFeePercent = feePercent,
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

            var snapshot = new
            {
                ContractVersion = 1,
                Type = ContractType.Rent,
                PostId = post.PostId,
                ProposalId = proposal.ProposalId,
                TenantId = tenantUserId,
                LandlordUserId = landlordUserId,
                StartDate = start,
                EndDate = end,
                MonthlyAmount = monthlyAmount,
                PaymentPlanId = plan.PaymentPlanId,
                PlatformFeePercent = feePercent,
                CreatedAt = DateTime.UtcNow
            };

            var contractId = await _contracts.CreateDraftAsync(
                postId: post.PostId,
                tenantId: tenantUserId,
                landlordUserId: landlordUserId,
                proposalId: proposal.ProposalId,
                type: ContractType.Rent,
                snapshot: snapshot
            );

            var contract = await _uow.Contracts.GetByIdAsync(contractId);

            plan.ContractId = contractId;
            plan.ContractHash = contract?.ContractHash;
            _uow.PaymentPlans.Update(plan);
            await _uow.CompleteAsync();

            var tx = new Transaction
            {
                UserId = tenantUserId,
                PostId = post.PostId,
                ProposalId = proposal.ProposalId,
                Amount = monthlyAmount,
                PaymentMethod = "Card",
                Status = TransactionStatus.installment,
                State = TransactionState.AwaitingSignatures,
                Kind = TransactionKind.Rent,
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
                message = "Rent initiated. Contract draft created. Sign then finalize to pay first month.",
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
