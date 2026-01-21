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
    public class SaleCashFlowService : ISaleCashFlowService
    {
        private readonly IUnitOfWork _uow;
        private readonly IEncryptionService _enc;
        private readonly IMockBankCardVault _bank;
        private readonly IContractService _contracts;
        private readonly IPaymentFlowHelpers _h;

        public SaleCashFlowService(
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

            // idempotency
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

            // ✅ لازم يكون UnderNegotiation (proposal اتقبل قبل الدفع)
            if (post.Status != PropertyStatus.UnderNegotiation)
                return new { success = false, message = "Post is not ready for payment" };

            var proposal = await _uow.Proposals.GetByIdAsync(dto.ProposalId);
            if (proposal == null) return new { success = false, message = "Proposal not found" };

            if (proposal.PostId != post.PostId)
                return new { success = false, message = "Proposal does not belong to this post" };

            if (proposal.TenantId != userId)
                return new { success = false, message = "You are not the owner of this proposal" };

            // ✅ لازم Approved قبل الدفع
            if (proposal.ProposalStatus != ProposalStatus.Approved)
                return new { success = false, message = "Proposal must be approved before payment" };

            var paymentCard = await _uow.PaymentCards.GetByIdAsync(dto.PaymentCardId);
            if (paymentCard == null || !paymentCard.IsActive)
                return new { success = false, message = "Payment card not found/active" };

            if (paymentCard.UserId != userId)
                return new { success = false, message = "You do not own this card" };

            if (string.IsNullOrWhiteSpace(dto.CVV))
                return new { success = false, message = "CVV is required" };

            decimal total;

            // ✅ determine total price (auction vs normal)
            if (post.IsAuction)
            {
                if (!proposal.Offeredprice.HasValue || proposal.Offeredprice.Value <= 0)
                    return new { success = false, message = "Auction requires Offeredprice in proposal" };

                total = (decimal)proposal.Offeredprice.Value;

                // ✅ lock final price on post
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

            var tx = new Transaction
            {
                UserId = userId,
                PostId = post.PostId,
                ProposalId = proposal.ProposalId, // ✅ NEW
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
                    return new { success = false, message = "Seller not found" };

                var landlordUserId = landlord.UserId;
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
                var fee = Math.Round(total * feePercent / 100m, 2);
                var net = total - fee;

                var res = await _bank.TransferWithFeeAsync(payerToken, dto.CVV, total, payeeToken, adminToken, fee);

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

                // contract draft snapshot
                var snapshot = new
                {
                    ContractVersion = 1,
                    Type = ContractType.SaleCash,
                    PostId = post.PostId,
                    ProposalId = proposal.ProposalId,
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
                    proposalId: proposal.ProposalId,
                    type: ContractType.SaleCash,
                    snapshot: snapshot
                );

                var contract = await _uow.Contracts.GetByIdAsync(contractId);

                tx.ContractId = contractId;
                tx.ContractHash = contract?.ContractHash;
                tx.State = TransactionState.Succeeded;
                tx.LastError = null;
                _uow.Transactions.Update(tx);

                // ✅ final state
                post.Status = PropertyStatus.Sold;
                _uow.Posts.Update(post);

                await _uow.CompleteAsync();

                return new
                {
                    success = true,
                    message = "Purchased (cash) + Contract Draft created",
                    transactionId = tx.TransactionId,
                    contractId,
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
    }
}
