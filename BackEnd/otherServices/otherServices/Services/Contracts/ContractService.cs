using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using otherServices.Models;
using otherServices.Models.DTOs.Contracts;
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
            var json = JsonSerializer.Serialize(snapshot, new JsonSerializerOptions { WriteIndented = false });
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

        // =========================
        // ✅ MY CONTRACTS (cards)
        // =========================
        public async Task<object> GetMyContractsAsync(long requesterUserId)
        {
            var contracts = (await _uow.Contracts.FindAsync(c =>
                    c.TenantId == requesterUserId || c.LandlordUserId == requesterUserId))
                .OrderByDescending(c => c.CreatedAt)
                .ToList();

            if (contracts.Count == 0)
                return new { success = true, count = 0, contracts = new List<ContractCardDto>() };

            var contractIds = contracts.Select(c => c.ContractId).ToList();
            var sigs = (await _uow.ContractSignatures.FindAsync(s => contractIds.Contains(s.ContractId))).ToList();

            // batch load posts
            var postIds = contracts.Select(c => c.PostId).Distinct().ToList();
            var posts = await _uow.Posts.GetAllQueryable()
                .AsNoTracking()
                .Where(p => postIds.Contains(p.PostId))
                .ToListAsync();
            var postsMap = posts.ToDictionary(p => p.PostId, p => p);

            // batch load other party users
            var otherUserIds = contracts
                .Select(c => c.TenantId == requesterUserId ? c.LandlordUserId : c.TenantId)
                .Distinct()
                .ToList();

            var users = await _uow.Users.GetAllQueryable()
                .AsNoTracking()
                .Where(u => otherUserIds.Contains(u.UserId))
                .ToListAsync();
            var usersMap = users.ToDictionary(u => u.UserId, u => u);

            // batch load payment plans (one per contract)
            var plans = await _uow.PaymentPlans.GetAllQueryable()
                .AsNoTracking()
                .Where(pp => pp.ContractId.HasValue && contractIds.Contains(pp.ContractId.Value))
                .ToListAsync();
            var plansMap = plans
                .Where(p => p.ContractId.HasValue)
                .GroupBy(p => p.ContractId!.Value)
                .ToDictionary(g => g.Key, g => g.First());

            var cards = new List<ContractCardDto>();

            foreach (var c in contracts)
            {
                var myRole = c.TenantId == requesterUserId ? "Buyer" : "Seller";
                var otherId = c.TenantId == requesterUserId ? c.LandlordUserId : c.TenantId;

                usersMap.TryGetValue(otherId, out var otherUser);
                postsMap.TryGetValue(c.PostId, out var post);
                plansMap.TryGetValue(c.ContractId, out var plan);

                var buyerSigned = sigs.Any(s => s.ContractId == c.ContractId && s.SignerRole == SignerRole.Buyer);
                var sellerSigned = sigs.Any(s => s.ContractId == c.ContractId && s.SignerRole == SignerRole.Seller);

                var nextAction = "NONE";
                if (c.Status != ContractStatus.FullySigned)
                {
                    if (myRole == "Buyer" && !buyerSigned) nextAction = "SIGN";
                    else if (myRole == "Seller" && !sellerSigned) nextAction = "SIGN";
                    else nextAction = "WAIT_OTHER_PARTY";
                }
                else
                {
                    if (myRole == "Buyer") nextAction = "FINALIZE";
                    else nextAction = "NONE";
                }

                var card = new ContractCardDto
                {
                    ContractId = c.ContractId,
                    Type = c.Type.ToString(),
                    Status = c.Status.ToString(),
                    CreatedAt = c.CreatedAt,

                    OtherParty = new ContractOtherPartyDto
                    {
                        UserId = otherUser?.UserId ?? otherId,
                        Name = otherUser?.UserName ?? "Unknown",
                        Phone = otherUser?.Phone ?? string.Empty
                    },

                    Property = new ContractCardPropertyDto
                    {
                        PostId = c.PostId,
                        Title = post?.Title ?? string.Empty,
                        Type = post?.Type.ToString() ?? string.Empty,
                        Rooms = post?.NumberOfRooms ?? 0,
                        Baths = post?.NumberOfBathrooms ?? 0,
                        Area = post?.Area ?? 0,
                        Price = post?.Price
                    },

                    // ✅ NEW: PaymentPlan summary on card (null for SaleCash or missing plan)
                    PaymentPlan = MapCardPlanSummary(c, plan),

                    Signatures = new ContractCardSignaturesDto
                    {
                        BuyerSigned = buyerSigned,
                        SellerSigned = sellerSigned
                    },

                    Ui = new ContractCardUiDto
                    {
                        MyRole = myRole,
                        NextAction = nextAction
                    }
                };

                cards.Add(card);
            }

            return new { success = true, count = cards.Count, contracts = cards };
        }

        // =========================
        // ✅ CONTRACT DETAILS DTO
        // =========================
        public async Task<ContractDetailsResponseDto> GetContractForUserAsync(long contractId, long requesterUserId)
        {
            var contract = await _uow.Contracts.GetByIdAsync(contractId);
            if (contract == null)
                return new ContractDetailsResponseDto { Success = false };

            if (contract.TenantId != requesterUserId && contract.LandlordUserId != requesterUserId)
                return new ContractDetailsResponseDto { Success = false };

            var tenantUser = await _uow.Users.GetByIdAsync(contract.TenantId);
            var landlordUser = await _uow.Users.GetByIdAsync(contract.LandlordUserId);
            var post = await _uow.Posts.GetByIdAsync(contract.PostId);

            // ✅ plan (one per contract)
            var plan = await _uow.PaymentPlans.GetAllQueryable()
                .AsNoTracking()
                .FirstOrDefaultAsync(pp => pp.ContractId == contractId);

            var sigs = (await _uow.ContractSignatures.FindAsync(s => s.ContractId == contractId)).ToList();

            var buyerSig = sigs
                .Where(s => s.SignerRole == SignerRole.Buyer)
                .OrderByDescending(s => s.SignedAt)
                .FirstOrDefault();

            var sellerSig = sigs
                .Where(s => s.SignerRole == SignerRole.Seller)
                .OrderByDescending(s => s.SignedAt)
                .FirstOrDefault();

            var myRole = contract.TenantId == requesterUserId ? "Buyer" : "Seller";

            var buyerSigned = buyerSig != null;
            var sellerSigned = sellerSig != null;

            // UI flags
            var canSign = false;
            var canFinalize = false;
            var nextAction = "NONE";

            if (contract.Status != ContractStatus.FullySigned)
            {
                if (myRole == "Buyer")
                {
                    canSign = !buyerSigned;
                    nextAction = canSign ? "SIGN" : "WAIT_OTHER_PARTY";
                }
                else
                {
                    canSign = !sellerSigned;
                    nextAction = canSign ? "SIGN" : "WAIT_OTHER_PARTY";
                }
            }
            else
            {
                // tenant only finalizes
                if (myRole == "Buyer")
                {
                    canFinalize = true;
                    nextAction = "FINALIZE";
                }
                else
                {
                    nextAction = "NONE";
                }
            }

            var dto = new ContractDetailsResponseDto
            {
                Success = true,

                Contract = new ContractHeaderDto
                {
                    ContractId = contract.ContractId,
                    ProposalId = contract.ProposalId,
                    PostId = contract.PostId,
                    Type = contract.Type.ToString(),
                    Status = contract.Status.ToString(),
                    ContractHash = contract.ContractHash,
                    Version = contract.Version,
                    CreatedAt = contract.CreatedAt
                },

                Parties = new ContractPartiesDto
                {
                    Tenant = new ContractPartyDto
                    {
                        UserId = tenantUser?.UserId ?? contract.TenantId,
                        Name = tenantUser?.UserName ?? "Unknown",
                        Email = tenantUser?.Email ?? string.Empty,
                        Phone = tenantUser?.Phone ?? string.Empty,
                        Address = tenantUser?.Address ?? string.Empty
                    },
                    Landlord = new ContractPartyDto
                    {
                        UserId = landlordUser?.UserId ?? contract.LandlordUserId,
                        Name = landlordUser?.UserName ?? "Unknown",
                        Email = landlordUser?.Email ?? string.Empty,
                        Phone = landlordUser?.Phone ?? string.Empty,
                        Address = landlordUser?.Address ?? string.Empty
                    }
                },

                Property = new ContractPropertyDto
                {
                    PostId = post?.PostId ?? contract.PostId,
                    Title = post?.Title ?? string.Empty,
                    Description = post?.Description ?? string.Empty,
                    Type = post?.Type.ToString() ?? string.Empty,
                    Status = post?.Status.ToString() ?? string.Empty,
                    Location = post?.Location ?? string.Empty,
                    Price = post?.Price,
                    IsAuction = post?.IsAuction ?? false,
                    NumberOfRooms = post?.NumberOfRooms ?? 0,
                    NumberOfBathrooms = post?.NumberOfBathrooms ?? 0,
                    Area = post?.Area ?? 0,
                    TotalUnitsInBuilding = post?.TotalUnitsInBuilding,
                    IsFurnished = post?.IsFurnished ?? false,
                    HasGarage = post?.HasGarage ?? false,
                    FloorNumber = post?.FloorNumber
                },

                // ✅ NEW: PaymentPlan DTO (null for SaleCash or missing)
                PaymentPlan = MapDetailsPlan(contract, plan),

                Signatures = new ContractSignaturesDto
                {
                    Buyer = new ContractSignatureStateDto
                    {
                        Signed = buyerSig != null,
                        SignedAt = buyerSig?.SignedAt,
                        SignatureId = buyerSig?.ContractSignatureId
                    },
                    Seller = new ContractSignatureStateDto
                    {
                        Signed = sellerSig != null,
                        SignedAt = sellerSig?.SignedAt,
                        SignatureId = sellerSig?.ContractSignatureId
                    }
                },

                Ui = new ContractUiDto
                {
                    MyRole = myRole,
                    CanSign = canSign,
                    CanFinalize = canFinalize,
                    NextAction = nextAction
                }
            };

            return dto;
        }

        // =========================
        // ✅ SIGN
        // =========================
        public async Task<object> SignAsync(long contractId, long signerUserId, SignerRole role, string? ip, string? userAgent)
        {
            var contract = await _uow.Contracts.GetByIdAsync(contractId);
            if (contract == null)
                return new { success = false, message = "Contract not found" };

            if (contract.Status == ContractStatus.Cancelled)
                return new { success = false, message = "Contract is cancelled" };

            // integrity
            var recomputed = Sha256Hex(contract.ContractJson);
            if (!string.Equals(recomputed, contract.ContractHash, StringComparison.OrdinalIgnoreCase))
                return new { success = false, message = "Contract integrity check failed (hash mismatch)" };

            // auth by role
            if (role == SignerRole.Buyer && signerUserId != contract.TenantId)
                return new { success = false, message = "Only buyer can sign as Buyer" };

            if (role == SignerRole.Seller && signerUserId != contract.LandlordUserId)
                return new { success = false, message = "Only seller can sign as Seller" };

            // prevent duplicate
            var existingByRole = await _uow.ContractSignatures.FirstOrDefaultAsync(s =>
                s.ContractId == contractId && s.SignerRole == role);

            if (existingByRole != null)
            {
                return new
                {
                    success = true,
                    message = "Already signed for this role",
                    signatureId = existingByRole.ContractSignatureId,
                    contractStatus = contract.Status.ToString()
                };
            }

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
                SignedPayload = payload,
                IpAddress = ip,
                UserAgent = userAgent
            };

            await _uow.ContractSignatures.AddAsync(sigEntity);
            await _uow.CompleteAsync();

            // update contract status
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

        // =========================
        // ✅ VERIFY
        // =========================
        public async Task<object> VerifyAsync(long contractId)
        {
            var contract = await _uow.Contracts.GetByIdAsync(contractId);
            if (contract == null)
                return new { success = false, message = "Contract not found" };

            var secret = _cfg["Contracts:ServerSigningSecret"];
            if (string.IsNullOrWhiteSpace(secret))
                return new { success = false, message = "Contracts:ServerSigningSecret not configured" };

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

        // =========================
        // ✅ Mapping helpers (NEW)
        // =========================

        private ContractPaymentPlanDto? MapDetailsPlan(Contract contract, PaymentPlan? plan)
        {
            // SaleCash -> no plan
            if (contract.Type == ContractType.SaleCash) return null;
            if (plan == null) return null;

            var dto = new ContractPaymentPlanDto
            {
                Status = plan.Status.ToString(),
                PlatformFeePercent = plan.PlatformFeePercent,
                PeriodicAmount = plan.PeriodicAmount,
                TotalAmount = plan.TotalAmount
            };

            // Rent
            if (contract.Type == ContractType.Rent)
            {
                dto.PlanType = "Rent";
                dto.StartDate = plan.StartDate;
                dto.EndDate = plan.EndDate;

                if (plan.StartDate.HasValue && plan.EndDate.HasValue)
                {
                    dto.DurationMonths = CalculateRentDurationMonths(plan.StartDate.Value.Date, plan.EndDate.Value.Date);
                    dto.PaymentsCount = dto.DurationMonths;
                }

                // rent is monthly by design
                dto.IntervalMonths = 1;
                dto.FrequencyLabel = "Monthly";
                return dto;
            }

            // SaleInstallment
            if (contract.Type == ContractType.SaleInstallment)
            {
                dto.PlanType = "SaleInstallment";

                dto.DurationMonths = plan.DurationMonths;
                dto.IntervalMonths = plan.IntervalMonths;
                dto.FrequencyLabel = IntervalMonthsToFrequencyLabel(plan.IntervalMonths);

                if (plan.DurationMonths.HasValue && plan.IntervalMonths.HasValue && plan.IntervalMonths.Value > 0)
                    dto.PaymentsCount = plan.DurationMonths.Value / plan.IntervalMonths.Value;

                return dto;
            }

            // fallback
            dto.PlanType = contract.Type.ToString();
            return dto;
        }

        private ContractCardPlanSummaryDto? MapCardPlanSummary(Contract contract, PaymentPlan? plan)
        {
            if (contract.Type == ContractType.SaleCash) return null;
            if (plan == null) return null;

            var dto = new ContractCardPlanSummaryDto
            {
                PeriodicAmount = plan.PeriodicAmount
            };

            if (contract.Type == ContractType.Rent)
            {
                dto.StartDate = plan.StartDate;
                dto.EndDate = plan.EndDate;

                if (plan.StartDate.HasValue && plan.EndDate.HasValue)
                    dto.DurationMonths = CalculateRentDurationMonths(plan.StartDate.Value.Date, plan.EndDate.Value.Date);

                return dto;
            }

            if (contract.Type == ContractType.SaleInstallment)
            {
                dto.IntervalMonths = plan.IntervalMonths;
                dto.FrequencyLabel = IntervalMonthsToFrequencyLabel(plan.IntervalMonths);
                dto.DurationMonths = plan.DurationMonths;

                if (plan.DurationMonths.HasValue && plan.IntervalMonths.HasValue && plan.IntervalMonths.Value > 0)
                    dto.PaymentsCount = plan.DurationMonths.Value / plan.IntervalMonths.Value;

                return dto;
            }

            return null;
        }

        // ✅ Rent duration months calculator
        // يحسب الفرق "بالشهور التقويمية" من غير +1 (علشان Feb07 -> May07 = 3 شهور)
        private static int CalculateRentDurationMonths(DateTime start, DateTime end)
        {
            if (end <= start) return 0;

            var months = (end.Year - start.Year) * 12 + (end.Month - start.Month);

            // لو end.Day أقل من start.Day يبقى الشهر الأخير مش كامل
            if (end.Day < start.Day) months -= 1;

            if (months < 1) months = 1;
            return months;
        }

        // ✅ IntervalMonths -> Frequency Label
        private static string? IntervalMonthsToFrequencyLabel(int? intervalMonths)
        {
            if (!intervalMonths.HasValue) return null;

            return intervalMonths.Value switch
            {
                1 => "Monthly",
                3 => "Quarterly",
                6 => "SemiAnnual",
                12 => "Annual",
                _ => $"{intervalMonths.Value} months"
            };
        }

        // =========================
        // Hash helpers
        // =========================
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
