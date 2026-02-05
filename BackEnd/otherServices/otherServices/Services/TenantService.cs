using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using otherServices.Infrastructure.Kafka;          // ✅ NEW
using otherServices.Models;
using otherServices.Models.DTOs;
using otherServices.Models.DTOs.Posts;
using otherServices.Models.Enums;
using otherServices.Repositories;

namespace otherServices.Services
{
    public class TenantService : ITenantService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IUserRepository _userRepository;
        private readonly IProposalRepository _proposalRepository;
        private readonly IPostRepository _postRepository;
        private readonly ISavedPostRepository _savedPostRepository;
        private readonly IMediaService _mediaService;
        private readonly AppDbContext2 _context;

        private readonly IAiRequestDispatcher _aiRequestDispatcher; // ✅ NEW

        public TenantService(
            AppDbContext2 context,
            IMediaService mediaService,
            IWebHostEnvironment env,
            IProposalRepository proposalRepository,
            IPostRepository postRepository,
            ISavedPostRepository savedPostRepository,
            IUserRepository userRepository,
            IAiRequestDispatcher aiRequestDispatcher // ✅ NEW
            )
        {
            _env = env;
            _proposalRepository = proposalRepository;
            _postRepository = postRepository;
            _savedPostRepository = savedPostRepository;
            _userRepository = userRepository;
            _mediaService = mediaService;
            _context = context;

            _aiRequestDispatcher = aiRequestDispatcher; // ✅ NEW
        }

        // ✅ IMPORTANT:
        // ResetEligibilityFields = "نصفر نتيجة تقييم الـ AI"
        // ❌ لكن ممنوع نمسح إجابات المستخدم (EligibilityAnswersJson)
        // لأنها جزء من البروپوزال ولازم تفضل محفوظة.
        private static void ResetEligibilityFields(Proposal proposal)
        {
            // installment AI decision
            proposal.IsAble = AIInstallmentDecision.NotCertain;

            // rent AI decision
            proposal.RentIsAble = AIRentDecision.NotCertain;

            // ❌ متتمسحش إجابات المستخدم
            // proposal.EligibilityAnswersJson = null;

            // optional AI outputs later
            proposal.EligibilityScore = null;
            proposal.EligibilityReason = null;
            proposal.EligibilityAssessedAt = null;

            // ✅ (اختياري) لو عندك RentEligibilityFields منفصلة
            proposal.RentEligibilityScore = null;
            proposal.RentEligibilityReason = null;
            proposal.RentEligibilityAssessedAt = null;
        }

        // ✅ validate eligibility json format (optional but safe)
        private static void EnsureValidJsonIfProvided(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return;

            try
            {
                JsonDocument.Parse(json);
            }
            catch
            {
                throw new ArgumentException("EligibilityAnswersJson must be a valid JSON string.");
            }
        }

        #region Posts (Tenant browse)
        public async Task<IEnumerable<PostSummaryDto>> GetPostsAsync()
        {
            var posts = await _postRepository.NestedFind(
                p => p.PendingStatus == PostPendingStatus.Accepted
                  && p.Status != PropertyStatus.Sold,
                p => p.Landlord,
                p => p.Landlord.User,
                p => p.PostImages
            );

            if (!posts.Any())
                throw new KeyNotFoundException("No posts found.");

            return posts.Select(p => new PostSummaryDto
            {
                PostId = p.PostId,
                UserId = p.Landlord?.UserId ?? 0,
                UserName = p.Landlord?.User?.UserName ?? "Unknown",
                Title = p.Title,
                Description = p.Description,
                Price = (double)(p.Price ?? 0),
                DatePost = p.CreatedAt,
                Images = p.PostImages?.Select(img => img.ImageUrl).ToList() ?? new List<string>()
            }).ToList();
        }
        #endregion

        #region Saved Posts
        public async Task<List<PostSummaryDto>> GetMySavedPosts(long userId)
        {
            var savedPosts = await _savedPostRepository.NestedFind(
                sp => sp.UserId == userId,
                sp => sp.Post,
                sp => sp.Post.Landlord,
                sp => sp.Post.Landlord.User,
                sp => sp.Post.PostImages
            );

            if (!savedPosts.Any())
                throw new KeyNotFoundException("No saved posts found.");

            return savedPosts.Select(sp =>
            {
                var post = sp.Post;
                return new PostSummaryDto
                {
                    PostId = post.PostId,
                    UserId = post.Landlord?.UserId ?? 0,
                    UserName = post.Landlord?.User?.UserName ?? "Unknown",
                    Title = post.Title,
                    Description = post.Description,
                    Price = (double)(post.Price ?? 0),
                    DatePost = post.CreatedAt,
                    Images = post.PostImages?.Select(pi => pi.ImageUrl).ToList() ?? new List<string>()
                };
            }).ToList();
        }

        public async Task Save_Post(long userId, long postId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null) throw new Exception("User not found");

            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null) throw new Exception("Post not found");

            var exists = (await _savedPostRepository.FindAsync(sp => sp.UserId == userId && sp.PostId == postId)).Any();
            if (exists) throw new Exception("Post is already Saved");

            await _savedPostRepository.AddAsync(new SavedPost { UserId = userId, PostId = postId });
            await _savedPostRepository.SaveChangesAsync();
        }

        public async Task CancelSave(long userId, long postId)
        {
            var sp = (await _savedPostRepository.FindAsync(x => x.UserId == userId && x.PostId == postId)).FirstOrDefault();
            if (sp == null) throw new Exception("Post not found in Your Saves");

            _savedPostRepository.Remove(sp);
            await _savedPostRepository.SaveChangesAsync();
        }
        #endregion

        #region Proposals

        public async Task<IEnumerable<ProposalDto>> GetTenantProposalsAsync(long tenantId)
        {
            var proposals = await _proposalRepository.NestedFind(
                p => p.TenantId == tenantId,
                p => p.User,
                p => p.Post,
                p => p.Post.Landlord,
                p => p.Post.Landlord.User,
                p => p.Post.PostImages
            );

            if (!proposals.Any())
                throw new KeyNotFoundException("No proposals found.");

            return proposals.Select(proposal =>
            {
                var post = proposal.Post!;
                var landlord = post.Landlord!;
                var landlordUser = landlord.User;

                return new ProposalDto
                {
                    ProposalId = proposal.ProposalId,
                    PostId = proposal.PostId,
                    Title = post.Title,
                    Description = post.Description,
                    ImagePath = post.PostImages?.FirstOrDefault()?.ImageUrl ?? string.Empty,

                    TenantId = proposal.TenantId,
                    TenantName = proposal.User?.UserName ?? "Unknown",
                    Phone = proposal.Phone,
                    StartRentalDate = proposal.StartRentalDate,
                    EndRentalDate = proposal.EndRentalDate,
                    ProposalStatus = proposal.ProposalStatus,
                    IsInstallment = proposal.IsInstallment,
                    FilePath = proposal.FilePath,
                    OfferedPrice = proposal.Offeredprice ?? 0,

                    LandlordUserId = landlordUser?.UserId ?? 0,
                    LandlordName = landlordUser?.UserName ?? "Unknown",

                    IsAble = proposal.IsAble,
                    RentIsAble = proposal.RentIsAble,
                    EligibilityScore = proposal.EligibilityScore,
                    EligibilityReason = proposal.EligibilityReason,
                    EligibilityAssessedAt = proposal.EligibilityAssessedAt
                };
            }).ToList();
        }

        public async Task SubmitProposalAsync(long TenantId, long PostId, SubmitProposalDto form)
        {
            var posts = await _postRepository.NestedFind(
                p => p.PostId == PostId,
                p => p.Landlord,
                p => p.Landlord.User,
                p => p.PostImages
            );

            var post = posts.FirstOrDefault();
            if (post == null) throw new KeyNotFoundException("Post not found");

            if (post.PendingStatus != PostPendingStatus.Accepted)
                throw new Exception("Post is not approved by admin.");

            if (post.Status == PropertyStatus.Sold)
                throw new Exception("Property is Sold.");

            // ✅ stop everything if already approved
            var anyApproved = await _proposalRepository.FirstOrDefaultAsync(p =>
                p.PostId == PostId && p.ProposalStatus == ProposalStatus.Approved);

            if (anyApproved != null)
                throw new Exception("This post already has an approved proposal. No more proposals are allowed.");

            // ✅ one proposal rule: allow again only if rejected
            var existing = await _proposalRepository.FirstOrDefaultAsync(p =>
                p.PostId == PostId &&
                p.TenantId == TenantId &&
                p.ProposalStatus != ProposalStatus.Rejected);

            if (existing != null)
                throw new InvalidOperationException("You already submitted a proposal for this post. You can only submit again if it was rejected.");

            // ✅ type rules (rent/sale)
            if (post.Type == PropertyType.Rent)
            {
                if (!form.StartRentalDate.HasValue || !form.EndRentalDate.HasValue)
                    throw new ArgumentException("Rental dates are required for rent properties.");

                if (form.IsInstallment != IsInstallment.Cash)
                    throw new ArgumentException("Installment is not allowed for rent properties.");
            }
            else if (post.Type == PropertyType.Sale)
            {
                if (form.StartRentalDate.HasValue || form.EndRentalDate.HasValue)
                    throw new ArgumentException("Rental dates are not allowed for sale properties.");
            }

            // ✅ auction vs normal rules
            if (post.IsAuction)
            {
                if (!form.Offeredprice.HasValue || form.Offeredprice.Value <= 0)
                    throw new ArgumentException("Offeredprice is required for auction posts and must be > 0.");
            }
            else
            {
                if (form.Offeredprice.HasValue)
                    throw new ArgumentException("Offeredprice is not allowed for non-auction posts.");

                if (!post.Price.HasValue || post.Price.Value <= 0)
                    throw new Exception("Post price is missing. Cannot submit proposal for non-auction post.");
            }

            // ✅ Eligibility required only for:
            // - Rent proposals
            // - Sale Installment proposals
            var requiresEligibility =
                (post.Type == PropertyType.Rent) ||
                (post.Type == PropertyType.Sale && form.IsInstallment == IsInstallment.Installment);

            if (requiresEligibility)
            {
                if (string.IsNullOrWhiteSpace(form.EligibilityAnswersJson))
                    throw new ArgumentException("EligibilityAnswersJson is required for rent or installment proposals.");

                EnsureValidJsonIfProvided(form.EligibilityAnswersJson);
            }
            else
            {
                // Sale cash => disallow
                if (!string.IsNullOrWhiteSpace(form.EligibilityAnswersJson))
                    throw new ArgumentException("EligibilityAnswersJson is not allowed for cash sale proposals.");
            }

            // file
            var filePath = await _mediaService.SaveFileAsync(form.File);
            if (string.IsNullOrEmpty(filePath))
                throw new Exception("File saving failed");

            var proposal = new Proposal
            {
                PostId = PostId,
                TenantId = TenantId,
                Phone = form.Phone,
                StartRentalDate = post.Type == PropertyType.Rent ? form.StartRentalDate : null,
                EndRentalDate = post.Type == PropertyType.Rent ? form.EndRentalDate : null,

                IsInstallment = post.Type == PropertyType.Sale ? form.IsInstallment : IsInstallment.Cash,
                Offeredprice = post.IsAuction ? form.Offeredprice : null,

                FilePath = filePath,
                ProposalStatus = ProposalStatus.Waiting,

                // ✅ store eligibility answers in Proposal
                EligibilityAnswersJson = requiresEligibility ? form.EligibilityAnswersJson : null
            };

            // ✅ reset AI output fields (but keep the answers)
            ResetEligibilityFields(proposal);

            await _proposalRepository.AddAsync(proposal);

            // keep your behavior
            post.Status = PropertyStatus.UnderNegotiation;

            // ✅ HighestOfferOnPost (Waiting only)
            var highest = await GetHighestWaitingOfferForPostAsync(PostId, post.IsAuction, post.Price);
            proposal.HighestOfferOnPost = highest;

            // ✅ optional: sync across all waiting proposals
            if (post.IsAuction && highest.HasValue)
            {
                var allWaiting = await _proposalRepository.FindAsync(p =>
                    p.PostId == PostId &&
                    p.ProposalStatus == ProposalStatus.Waiting);

                foreach (var p in allWaiting)
                    p.HighestOfferOnPost = highest;
            }

            // ✅ NEW: Transaction لضمان:
            // - proposalId يتولد
            // - outbox يتكتب بنفس العملية
            await using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1) احفظ علشان يطلع ProposalId
                await _context.SaveChangesAsync();

                // 2) لو محتاج AI Eligibility ابعت request عبر outbox
                if (requiresEligibility)
                {
                    // payload اللي هيبقى رايح للـ AI
                    // (خليه بسيط: answers + context)
                    var payload = new
                    {
                        tenantId = TenantId,
                        postId = PostId,
                        propertyType = post.Type.ToString(),
                        isInstallment = form.IsInstallment.ToString(),
                        offeredPrice = proposal.Offeredprice,
                        postPrice = post.Price,
                        startRentalDate = proposal.StartRentalDate,
                        endRentalDate = proposal.EndRentalDate,
                        eligibilityAnswersJson = proposal.EligibilityAnswersJson
                    };

                    // Rent eligibility
                    if (post.Type == PropertyType.Rent)
                    {
                        await _aiRequestDispatcher.EnqueueAsync(
                            requestType: AiRequestTypes.Buyer_RentEligibility,
                            entityType: "proposal",
                            entityId: proposal.ProposalId,
                            payload: payload
                        );
                    }
                    // Sale installment risk
                    else if (post.Type == PropertyType.Sale && proposal.IsInstallment == IsInstallment.Installment)
                    {
                        await _aiRequestDispatcher.EnqueueAsync(
                            requestType: AiRequestTypes.Buyer_InstallmentRisk,
                            entityType: "proposal",
                            entityId: proposal.ProposalId,
                            payload: payload
                        );
                    }

                    // 3) احفظ outbox
                    await _context.SaveChangesAsync();
                }

                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                throw new Exception(ex.InnerException?.Message ?? ex.Message);
            }
        }

        public async Task DeleteProposalAsync(long proposalId)
        {
            var proposal = await _proposalRepository.GetByIdAsync(proposalId);
            if (proposal == null) throw new KeyNotFoundException("Proposal not found");

            if (!string.IsNullOrEmpty(proposal.FilePath) && File.Exists(proposal.FilePath))
                File.Delete(proposal.FilePath);

            _proposalRepository.Remove(proposal);
            await _proposalRepository.SaveChangesAsync();
        }

        public async Task EditProposalAsync(long proposalId, ProposalEditDto updated)
        {
            var proposal = await _proposalRepository.GetByIdAsync(proposalId);
            if (proposal == null) throw new KeyNotFoundException("Proposal not found");

            if (proposal.ProposalStatus != ProposalStatus.Waiting)
                throw new Exception("You can only edit a waiting proposal.");

            // ✅ stop everything if post already has approved proposal
            var anyApproved = await _proposalRepository.FirstOrDefaultAsync(p =>
                p.PostId == proposal.PostId && p.ProposalStatus == ProposalStatus.Approved);

            if (anyApproved != null)
                throw new Exception("This post already has an approved proposal. No more edits are allowed.");

            var post = await _postRepository.GetByIdAsync(proposal.PostId);
            if (post == null) throw new KeyNotFoundException("Post not found");

            // forbid document change
            if (updated.File != null)
                throw new Exception("You cannot update the property document in a proposal. Delete proposal and create a new one.");

            // forbid changing installment choice (for sale)
            if (updated.IsInstallment.HasValue && updated.IsInstallment.Value != proposal.IsInstallment)
                throw new Exception("You cannot change Installment/Cash after submitting. Delete proposal and create a new one.");

            // forbid changing rental dates
            if (updated.StartRentalDate.HasValue && updated.StartRentalDate.Value.Date != proposal.StartRentalDate?.Date)
                throw new Exception("You cannot change rental dates after submitting. Delete proposal and create a new one.");

            if (updated.EndRentalDate.HasValue && updated.EndRentalDate.Value.Date != proposal.EndRentalDate?.Date)
                throw new Exception("You cannot change rental dates after submitting. Delete proposal and create a new one.");

            // forbid changing offered price in auction
            if (post.IsAuction && updated.Offeredprice.HasValue)
            {
                var newPrice = updated.Offeredprice.Value;
                if (!proposal.Offeredprice.HasValue || proposal.Offeredprice.Value != newPrice)
                    throw new Exception("You cannot change Offeredprice after submitting. Delete proposal and create a new one.");
            }

            if (!post.IsAuction && updated.Offeredprice.HasValue)
                throw new Exception("Offeredprice not allowed for non-auction post.");

            // ✅ Allowed small edit:
            if (!string.IsNullOrEmpty(updated.Phone) && updated.Phone != proposal.Phone)
                proposal.Phone = updated.Phone;

            // Keep HighestOfferOnPost updated (optional)
            var highest = await GetHighestWaitingOfferForPostAsync(post.PostId, post.IsAuction, post.Price);
            proposal.HighestOfferOnPost = highest;

            if (post.IsAuction && highest.HasValue)
            {
                var allWaiting = await _proposalRepository.FindAsync(p =>
                    p.PostId == post.PostId &&
                    p.ProposalStatus == ProposalStatus.Waiting);

                foreach (var p in allWaiting)
                    p.HighestOfferOnPost = highest;
            }

            await _context.SaveChangesAsync();
        }

        private async Task<double?> GetHighestWaitingOfferForPostAsync(long postId, bool isAuction, double? postPrice)
        {
            if (!isAuction)
                return postPrice;

            var offers = await _proposalRepository.FindAsync(p =>
                p.PostId == postId &&
                p.ProposalStatus == ProposalStatus.Waiting &&
                p.Offeredprice.HasValue);

            if (!offers.Any()) return null;
            return offers.Max(p => p.Offeredprice!.Value);
        }

        #endregion

        public async Task UpgradeToLandlord(long userId, LandlordUpgradeRequestDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null) throw new Exception("User not found");

            if (user.RoleName != UserRole.Tenant)
                throw new Exception("User is already a landlord or admin");

            if (dto.OwnershipDoc == null || dto.OwnershipDoc.Length == 0)
                throw new Exception("Ownership document is required");

            string filePath = await _mediaService.SaveFileAsync(dto.OwnershipDoc);

            var landlord = new Landlord
            {
                UserId = userId,
                OwnershipDocPath = filePath,
                OwnershipDocPathEvaluation = AIDecision.Uncertain,
                PendingStatus = PendingStatus.Pending,
                Rate = 0
            };

            await _context.Landlords.AddAsync(landlord);

            user.RoleName = UserRole.Landlord;
            _context.Users.Update(user);

            // ✅ NEW: Transaction عشان LandlordId يطلع وبعدين نكتب outbox بنفس العملية
            await using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                // 1) احفظ علشان يطلع landlord.LandlordId (Identity)
                await _context.SaveChangesAsync();

                // 2) ابعت طلب فحص Ownership document للـ AI عن طريق outbox
                // entityType = "landlord" و entityId = LandlordId
                var payload = new
                {
                    userId = userId,
                    landlordId = landlord.LandlordId,
                    ownershipDocPath = landlord.OwnershipDocPath
                };

                await _aiRequestDispatcher.EnqueueAsync(
                    requestType: AiRequestTypes.Fraud_OwnershipDocumentAnalysis,
                    entityType: "landlord",
                    entityId: landlord.LandlordId,
                    payload: payload
                );

                // 3) احفظ outbox
                await _context.SaveChangesAsync();

                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                throw new Exception(ex.InnerException?.Message ?? ex.Message);
            }
        }
    }
}
