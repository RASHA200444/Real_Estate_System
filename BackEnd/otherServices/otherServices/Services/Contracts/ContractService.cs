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
                ProposalId = proposalId == 0 ? null : proposalId,
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

        public async Task<object> GetContractAsync(long contractId)
        {
            var contract = await _uow.Contracts.GetByIdAsync(contractId);
            if (contract == null)
                return new { success = false, message = "Contract not found" };

            var sigs = await _uow.ContractSignatures.FindAsync(s => s.ContractId == contractId);

            return new
            {
                success = true,
                contractId = contract.ContractId,
                contract.PostId,
                contract.ProposalId,
                contract.TenantId,
                contract.LandlordUserId,
                type = contract.Type.ToString(),
                status = contract.Status.ToString(),
                contract.ContractHash,
                contract.Version,
                contract.CreatedAt,
                signatures = sigs
                    .OrderBy(x => x.SignedAt)
                    .Select(x => new
                    {
                        x.ContractSignatureId,
                        x.SignerUserId,
                        role = x.SignerRole.ToString(),
                        x.SignedAt,
                        x.ContractHash,
                        x.SignatureAlgo,
                        x.SignatureValue,
                        x.SignedPayload,
                        x.IpAddress,
                        x.UserAgent
                    })
            };
        }

        public async Task<object> SignAsync(long contractId, long signerUserId, SignerRole role, string? ip, string? userAgent)
        {
            var contract = await _uow.Contracts.GetByIdAsync(contractId);
            if (contract == null)
                return new { success = false, message = "Contract not found" };

            if (contract.Status == ContractStatus.Cancelled)
                return new { success = false, message = "Contract is cancelled" };

            // ✅ Integrity check: prevent signing if JSON != hash
            var recomputed = Sha256Hex(contract.ContractJson);
            if (!string.Equals(recomputed, contract.ContractHash, StringComparison.OrdinalIgnoreCase))
                return new { success = false, message = "Contract integrity check failed (hash mismatch)" };

            // ✅ Authorization based on role
            if (role == SignerRole.Buyer && signerUserId != contract.TenantId)
                return new { success = false, message = "Only buyer can sign as Buyer" };

            if (role == SignerRole.Seller && signerUserId != contract.LandlordUserId)
                return new { success = false, message = "Only seller can sign as Seller" };

            // ✅ Prevent duplicate signature per role
            var existingByRole = await _uow.ContractSignatures.FirstOrDefaultAsync(s =>
                s.ContractId == contractId && s.SignerRole == role);

            if (existingByRole != null)
                return new
                {
                    success = true,
                    message = "Already signed for this role",
                    signatureId = existingByRole.ContractSignatureId,
                    contractStatus = contract.Status.ToString()
                };

            var secret = _cfg["Contracts:ServerSigningSecret"];
            if (string.IsNullOrWhiteSpace(secret))
                return new { success = false, message = "Contracts:ServerSigningSecret not configured" };

            var nonce = Guid.NewGuid().ToString("N");
            var signedAt = DateTime.UtcNow;

            // ✅ this exact string will be stored for later verification
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
                SignedPayload = payload,          // ✅ NEW
                IpAddress = ip,
                UserAgent = userAgent
            };

            await _uow.ContractSignatures.AddAsync(sigEntity);
            await _uow.CompleteAsync();

            // ✅ Update contract status
            var allSigs = await _uow.ContractSignatures.FindAsync(s => s.ContractId == contractId);

            bool buyerSigned = allSigs.Any(s => s.SignerRole == SignerRole.Buyer);
            bool sellerSigned = allSigs.Any(s => s.SignerRole == SignerRole.Seller);

            if (buyerSigned && sellerSigned)
                contract.Status = ContractStatus.FullySigned;
            else if (buyerSigned)
                contract.Status = ContractStatus.PendingSellerSignature;
            else if (sellerSigned)
                contract.Status = ContractStatus.PendingBuyerSignature;

            _uow.Contracts.Update(contract);
            await _uow.CompleteAsync();

            return new
            {
                success = true,
                message = "Signed",
                contractId,
                contractHash = contract.ContractHash,
                contractStatus = contract.Status.ToString(),
                signature = new
                {
                    sigEntity.ContractSignatureId,
                    sigEntity.SignerUserId,
                    role = sigEntity.SignerRole.ToString(),
                    sigEntity.SignedAt,
                    sigEntity.SignatureAlgo,
                    sigEntity.SignatureValue,
                    sigEntity.SignedPayload
                }
            };
        }

        public async Task<object> VerifyAsync(long contractId)
        {
            var contract = await _uow.Contracts.GetByIdAsync(contractId);
            if (contract == null)
                return new { success = false, message = "Contract not found" };

            var secret = _cfg["Contracts:ServerSigningSecret"];
            if (string.IsNullOrWhiteSpace(secret))
                return new { success = false, message = "Contracts:ServerSigningSecret not configured" };

            // 1) integrity
            var recomputed = Sha256Hex(contract.ContractJson);
            var integrityOk = string.Equals(recomputed, contract.ContractHash, StringComparison.OrdinalIgnoreCase);

            var sigs = await _uow.ContractSignatures.FindAsync(s => s.ContractId == contractId);

            bool VerifySig(ContractSignature s)
            {
                if (string.IsNullOrWhiteSpace(s.SignedPayload)) return false;
                var expected = HmacBase64(secret, s.SignedPayload);
                return string.Equals(expected, s.SignatureValue, StringComparison.Ordinal);
            }

            var sigChecks = sigs
                .OrderBy(x => x.SignedAt)
                .Select(s => new
                {
                    s.ContractSignatureId,
                    s.SignerUserId,
                    role = s.SignerRole.ToString(),
                    s.SignedAt,
                    algo = s.SignatureAlgo,
                    ok = VerifySig(s)
                })
                .ToList();

            var buyerOk = sigs.Where(x => x.SignerRole == SignerRole.Buyer).Any(x => VerifySig(x));
            var sellerOk = sigs.Where(x => x.SignerRole == SignerRole.Seller).Any(x => VerifySig(x));

            return new
            {
                success = true,
                contractId = contract.ContractId,
                status = contract.Status.ToString(),
                contractHash = contract.ContractHash,
                integrityOk,
                signatures = sigChecks,
                fullyVerifiable = integrityOk && buyerOk && sellerOk
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
