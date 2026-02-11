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
    public class SaleInstallmentFlowService : ISaleInstallmentFlowService
    {
        private const int AI_COOLDOWN_MINUTES = 5;

        private readonly IUnitOfWork _uow;
        private readonly IContractService _contracts;
        private readonly IPaymentFlowHelpers _h;

        private readonly AppDbContext2 _context;
        private readonly IAiRequestDispatcher _ai;

        public SaleInstallmentFlowService(
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

            return null;
        }

        public async Task<object> ExecuteAsync(long userId, SaleInstallmentRequestDto dto)
        {
            var guard = await GuardIdentityForPaymentAsync(userId);
            if (guard != null) return guard;

            if (string.IsNullOrWhiteSpace(dto.ExternalRef))
                return new { success = false, message = "ExternalRef is required" };

            // ✅ idempotency by ExternalRef
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

            // ✅ SOURCE OF TRUTH: Proposal -> PostId
            var proposal = await _uow.Proposals.GetByIdAsync(dto.ProposalId);
            if (proposal == null) return new { success = false, message = "Proposal not found" };

            if (proposal.TenantId != userId)
                return new { success = false, message = "You are not the owner of this proposal" };

            if (proposal.ProposalStatus != ProposalStatus.Approved)
                return new { success = false, message = "Proposal must be approved before initiating installment" };

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

            var post = await _uow.Posts.GetByIdAsync(proposal.PostId);
            if (post == null) return new { success = false, message = "Post not found" };

            if (post.PendingStatus != PostPendingStatus.Accepted)
                return new { success = false, message = "Post is not approved by admin" };

            if (post.Type != PropertyType.Sale)
                return new { success = false, message = "SaleInstallment is for SALE only." };

            if (post.Status == PropertyStatus.Sold)
                return new { success = false, message = "Post is sold" };

            if (post.Status != PropertyStatus.UnderNegotiation)
                return new { success = false, message = "Post is not ready for installment initiation" };

            // ✅ Card validation
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

                // keep post price consistent
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

            var firstSchedule = new PaymentSchedule
            {
                PaymentPlanId = plan.PaymentPlanId,
                DueDate = DateTime.UtcNow.Date,
                Amount = perPayment,
                IsPaid = false
            };

            await _uow.PaymentSchedules.AddAsync(firstSchedule);
            await _uow.CompleteAsync();

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

            plan.ContractId = contractId;
            plan.ContractHash = contract?.ContractHash;
            _uow.PaymentPlans.Update(plan);
            await _uow.CompleteAsync();

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
