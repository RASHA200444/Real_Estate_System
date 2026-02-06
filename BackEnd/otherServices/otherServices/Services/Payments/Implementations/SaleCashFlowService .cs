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
        private readonly IUnitOfWork _uow;
        private readonly IContractService _contracts;
        private readonly IPaymentFlowHelpers _h;

        public SaleCashFlowService(
            IUnitOfWork uow,
            IContractService contracts,
            IPaymentFlowHelpers helpers)
        {
            _uow = uow;
            _contracts = contracts;
            _h = helpers;
        }

        // ✅ NOW: Initiate ONLY (Create Contract Draft + Transaction AwaitingSignatures)
        public async Task<object> ExecuteAsync(long userId, SaleCashRequestDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.ExternalRef))
                return new { success = false, message = "ExternalRef is required" };

            // idempotency: if same externalRef exists, return it (do not create new)
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
                return new { success = false, message = "SaleCash is for SALE only." };

            if (post.Status == PropertyStatus.Sold)
                return new { success = false, message = "Post is sold" };

            if (post.Status != PropertyStatus.UnderNegotiation)
                return new { success = false, message = "Post is not ready for payment" };

            var proposal = await _uow.Proposals.GetByIdAsync(dto.ProposalId);
            if (proposal == null) return new { success = false, message = "Proposal not found" };

            if (proposal.PostId != post.PostId)
                return new { success = false, message = "Proposal does not belong to this post" };

            if (proposal.TenantId != userId)
                return new { success = false, message = "You are not the owner of this proposal" };

            if (proposal.ProposalStatus != ProposalStatus.Approved)
                return new { success = false, message = "Proposal must be approved before initiating payment" };

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

            // seller
            var landlord = await _uow.Landlords.GetByIdAsync(post.LandlordId);
            if (landlord == null)
                return new { success = false, message = "Seller not found" };

            var landlordUserId = landlord.UserId;

            // snapshot (no bank movement yet)
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
