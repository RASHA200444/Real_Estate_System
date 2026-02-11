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
    public class SaleCashFlowService : ISaleCashFlowService
    {
        private const int AI_COOLDOWN_MINUTES = 5;

        private readonly IUnitOfWork _uow;
        private readonly IContractService _contracts;
        private readonly IPaymentFlowHelpers _h;

        private readonly AppDbContext2 _context;
        private readonly IAiRequestDispatcher _ai;

        public SaleCashFlowService(
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

        public async Task<object> ExecuteAsync(long userId, SaleCashRequestDto dto)
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
                return new { success = false, message = "Proposal must be approved before initiating payment" };

            var post = await _uow.Posts.GetByIdAsync(proposal.PostId);
            if (post == null) return new { success = false, message = "Post not found" };

            if (post.PendingStatus != PostPendingStatus.Accepted)
                return new { success = false, message = "Post is not approved by admin" };

            if (post.Type != PropertyType.Sale)
                return new { success = false, message = "SaleCash is for SALE only." };

            if (post.Status == PropertyStatus.Sold)
                return new { success = false, message = "Post is sold" };

            if (post.Status != PropertyStatus.UnderNegotiation)
                return new { success = false, message = "Post is not ready for payment" };

            // ✅ Card validation
            var paymentCard = await _uow.PaymentCards.GetByIdAsync(dto.PaymentCardId);
            if (paymentCard == null || !paymentCard.IsActive)
                return new { success = false, message = "Payment card not found/active" };

            if (paymentCard.UserId != userId)
                return new { success = false, message = "You do not own this card" };

            decimal total;

            if (post.IsAuction)
            {
                if (!proposal.Offeredprice.HasValue || proposal.Offeredprice.Value <= 0)
                    return new { success = false, message = "Auction requires Offeredprice in proposal" };

                total = (decimal)proposal.Offeredprice.Value;

                // keep post price consistent (optional but you already do it)
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

            var landlord = await _uow.Landlords.GetByIdAsync(post.LandlordId);
            if (landlord == null)
                return new { success = false, message = "Seller not found" };

            var landlordUserId = landlord.UserId;

            var feePercent = _h.GetFeePercent();

            var snapshot = new
            {
                ContractVersion = 1,
                Type = ContractType.SaleCash,
                PostId = post.PostId,
                ProposalId = proposal.ProposalId,
                TenantId = userId,
                LandlordUserId = landlordUserId,
                Price = total,
                PlatformFeePercent = feePercent,
                CreatedAt = DateTime.UtcNow
            };

            var contractId = await _contracts.CreateDraftAsync(
                postId: post.PostId,
                tenantId: userId,
                landlordUserId: landlordUserId,
                proposalId: proposal.ProposalId,
                type: ContractType.SaleCash,
                snapshot: snapshot
            );

            var contract = await _uow.Contracts.GetByIdAsync(contractId);

            var tx = new Transaction
            {
                UserId = userId,
                PostId = post.PostId,
                ProposalId = proposal.ProposalId,
                Amount = total,
                PaymentMethod = "Card",
                Status = TransactionStatus.purchased,
                State = TransactionState.AwaitingSignatures,
                Kind = TransactionKind.SaleCash,
                ExternalRef = dto.ExternalRef,
                PaymentCardId = dto.PaymentCardId,
                Attempts = 0,
                CreatedAt = DateTime.UtcNow,
                ContractId = contractId,
                ContractHash = contract?.ContractHash
            };

            await _uow.Transactions.AddAsync(tx);
            await _uow.CompleteAsync();

            return new
            {
                success = true,
                message = "Contract draft created. Sign then finalize to pay.",
                transactionId = tx.TransactionId,
                externalRef = tx.ExternalRef,
                contractId,
                contractHash = tx.ContractHash,
                nextAction = "SIGN_CONTRACT_THEN_FINALIZE"
            };
        }
    }
}
