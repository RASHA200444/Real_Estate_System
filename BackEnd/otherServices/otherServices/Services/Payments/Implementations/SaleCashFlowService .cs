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

            // ✅ Constraints
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

            var total = (decimal)post.Price;

            // tx first
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

                // landlord
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
                var adminUserId = _h.GetAdminUserId();

                // landlord/admin cards
                var landlordCard = await _h.GetDefaultActiveCardAsync(landlordUserId);
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

                var adminCard = await _h.GetDefaultActiveCardAsync(adminUserId);
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

                // decrypt tokens
                var payerToken = _enc.Decrypt(paymentCard.CardTokenEncrypted);
                var payeeToken = _enc.Decrypt(landlordCard.CardTokenEncrypted);
                var adminToken = _enc.Decrypt(adminCard.CardTokenEncrypted);

                // fee
                var feePercent = _h.GetFeePercent();
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

                // contract draft
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

                await _h.CreateNotificationAsync(userId, $"Purchase successful for '{post.Title}'. Contract created (Draft).");
                await _h.CreateNotificationAsync(landlordUserId, $"Your property '{post.Title}' was sold (cash). Contract created (Draft).");

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
    }
}
