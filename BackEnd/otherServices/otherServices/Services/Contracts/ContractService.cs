using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using otherServices.Models;
using otherServices.Models.Enums;
using otherServices.Repositories;

namespace otherServices.Services.Contracts
{
    public class ContractService : IContractService
    {
        private readonly IUnitOfWork _uow;
        private readonly IConfiguration _cfg;

        public ContractService(IUnitOfWork uow, IConfiguration cfg)
        {
            _uow = uow;
            _cfg = cfg;
        }

        public async Task<long> CreateDraftAsync(long postId, long tenantId, long landlordUserId, long proposalId, ContractType type, object snapshot)
        {
            var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions
            {
                WriteIndented = false
            });

            var hash = Sha256Hex(json);

            var c = new Contract
            {
                PostId = postId,
                TenantId = tenantId,
                LandlordUserId = landlordUserId,
                ProposalId = proposalId,
                Type = type,
                Status = ContractStatus.Draft,
                ContractJson = json,
                ContractHash = hash,
                Version = 1,
                CreatedAt = DateTime.UtcNow
            };

            await _uow.Contracts.AddAsync(c);
            await _uow.CompleteAsync();

            return c.ContractId;
        }

        public async Task<object> SignAsync(long contractId, long signerUserId, SignerRole role, string? ip, string? userAgent)
        {
            var contract = await _uow.Contracts.GetByIdAsync(contractId);
            if (contract == null)
                return new { success = false, message = "Contract not found" };

            if (contract.Status == ContractStatus.Cancelled)
                return new { success = false, message = "Contract is cancelled" };

            if (contract.Status == ContractStatus.FullySigned)
                return new { success = true, message = "Already fully signed" };

            // ✅ Authorization check based on role
            if (role == SignerRole.Buyer && signerUserId != contract.TenantId)
                return new { success = false, message = "Only buyer can sign as Buyer" };

            if (role == SignerRole.Seller && signerUserId != contract.LandlordUserId)
                return new { success = false, message = "Only seller can sign as Seller" };

            // ✅ prevent duplicate signature
            var existing = await _uow.ContractSignatures.FirstOrDefaultAsync(s =>
                s.ContractId == contractId && s.SignerUserId == signerUserId);

            if (existing != null)
                return new { success = true, message = "Already signed", signatureId = existing.ContractSignatureId };

            // ✅ Server witnessed signature
            var secret = _cfg["Contracts:ServerSigningSecret"];
            if (string.IsNullOrWhiteSpace(secret))
                return new { success = false, message = "Contracts:ServerSigningSecret not configured" };

            var nonce = Guid.NewGuid().ToString("N");
            var signedAt = DateTime.UtcNow;

            var payload = $"{contract.ContractHash}|{contractId}|{signerUserId}|{role}|{signedAt:O}|{nonce}";
            var sigValue = HmacBase64(secret, payload);

            var sigEntity = new ContractSignature
            {
                ContractId = contractId,
                SignerUserId = signerUserId,
                SignerRole = role,
                SignedAt = signedAt,
                ContractHash = contract.ContractHash,
                SignatureAlgo = "ServerHMAC-SHA256",
                SignatureValue = sigValue,
                IpAddress = ip,
                UserAgent = userAgent
            };

            await _uow.ContractSignatures.AddAsync(sigEntity);
            await _uow.CompleteAsync();

            // ✅ Determine status after signing
            var allSigs = await _uow.ContractSignatures.FindAsync(s => s.ContractId == contractId);

            bool buyerSigned = allSigs.Any(s => s.SignerRole == SignerRole.Buyer);
            bool sellerSigned = allSigs.Any(s => s.SignerRole == SignerRole.Seller);

            if (buyerSigned && sellerSigned)
            {
                contract.Status = ContractStatus.FullySigned;
            }
            else if (buyerSigned)
            {
                contract.Status = ContractStatus.PendingSellerSignature;
            }
            else if (sellerSigned)
            {
                contract.Status = ContractStatus.PendingBuyerSignature;
            }

            _uow.Contracts.Update(contract);
            await _uow.CompleteAsync();

            return new
            {
                success = true,
                message = "Signed",
                contractId,
                contractHash = contract.ContractHash,
                contractStatus = contract.Status.ToString(),
                signatureAlgo = sigEntity.SignatureAlgo,
                signatureValue = sigEntity.SignatureValue,
                signedAt = sigEntity.SignedAt
            };
        }

        private static string Sha256Hex(string input)
        {
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }

        private static string HmacBase64(string secret, string payload)
        {
            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            var bytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
            return Convert.ToBase64String(bytes);
        }
    }
}
