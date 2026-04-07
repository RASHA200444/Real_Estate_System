using System.Diagnostics.Contracts;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using otherServices.Infrastructure.Kafka;
using otherServices.Models;
using otherServices.Models.DTOs;
using otherServices.Models.DTOs.Posts;
using otherServices.Models.Enums;
using otherServices.Repositories;
using otherServices.Services.Interfaces;
using RentMate.Services.Implementations;
using WebAPIDotNet.DTOs;

namespace otherServices.Services
{
    public class LandlordService : ILandlordService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILandlordRepository _landlordRepository;
        private readonly IUserRepository _userRepository;
        private readonly IPostRepository _postRepository;
        private readonly IProposalRepository _proposalRepository;
        private readonly IMediaService _mediaService;

        private readonly AppDbContext2 _context;
        private readonly IAiRequestDispatcher _aiRequestDispatcher;
        private readonly INotificationService _notificationService;

        public LandlordService(
            IWebHostEnvironment env,
            ILandlordRepository landlordRepository,
            IUserRepository userRepository,
            IPostRepository postRepository,
            IProposalRepository proposalRepository,
            IMediaService mediaService,
            AppDbContext2 context,
            IAiRequestDispatcher aiRequestDispatcher, 
            INotificationService notificationService)
        {
            _env = env;
            _landlordRepository = landlordRepository;
            _userRepository = userRepository;
            _postRepository = postRepository;
            _proposalRepository = proposalRepository;
            _mediaService = mediaService;
            _context = context;
            _aiRequestDispatcher = aiRequestDispatcher;
            _notificationService = notificationService;
        }

        // ============================================================
        // ✅ GUARDS
        // ============================================================

        private async Task<Landlord> GuardActiveLandlordAsync(long landlordUserId)
        {
            var user = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == landlordUserId);
            if (user == null) throw new Exception("User not found");

            if (user.RoleName != UserRole.Landlord)
                throw new Exception("Access denied: user is not a landlord");

            var landlord = await _context.Landlords
                .Include(l => l.User)
                .FirstOrDefaultAsync(l => l.UserId == landlordUserId);

            if (landlord == null)
                throw new Exception("Landlord profile not found");

            if (landlord.PendingStatus != PendingStatus.Active)
                throw new Exception("Landlord is not active (not approved yet)");

            return landlord;
        }

        private async Task<Post> GuardPostOwnedByLandlordAsync(long landlordUserId, long postId)
        {
            var landlord = await GuardActiveLandlordAsync(landlordUserId);

            var post = await _context.Posts
                .Include(p => p.PostImages)
                .FirstOrDefaultAsync(p => p.PostId == postId);

            if (post == null)
                throw new KeyNotFoundException("Post not found");

            if (post.LandlordId != landlord.LandlordId)
                throw new Exception("Access denied: this post does not belong to you");

            return post;
        }

        private async Task<Proposal> GuardProposalOwnedByLandlordAsync(long landlordUserId, long proposalId)
        {
            var landlord = await GuardActiveLandlordAsync(landlordUserId);

            var proposal = await _context.Proposals
                .Include(p => p.Post)
                .ThenInclude(x => x.Landlord)
                .FirstOrDefaultAsync(p => p.ProposalId == proposalId);

            if (proposal == null)
                throw new KeyNotFoundException("Proposal not found");

            if (proposal.Post == null)
                throw new Exception("Post not found for this proposal");

            if (proposal.Post.LandlordId != landlord.LandlordId)
                throw new Exception("Access denied: proposal is not for your post");

            return proposal;
        }

        // ============================================================
        // Posts
        // ============================================================

        public async Task<PostDTo> Get_Post_By_Id(long postId)
        {
            var posts = await _postRepository.NestedFind(
                p => p.PostId == postId,
                p => p.Landlord,
                p => p.Landlord.User,
                p => p.PostImages
            );

            var post = posts.FirstOrDefault();
            if (post == null) throw new KeyNotFoundException("Post not found");

            return MapToDTO(post, post.Landlord);
        }

        public async Task<List<PostSummaryDto>> GetMyPostsAsync(long landlordUserId)
        {
            var landlord = await GuardActiveLandlordAsync(landlordUserId);

            var posts = await _postRepository.NestedFind(
                p => p.LandlordId == landlord.LandlordId,
                p => p.Landlord,
                p => p.Landlord.User,
                p => p.PostImages
            );

            if (!posts.Any())
                throw new KeyNotFoundException("No posts found for this landlord.");

            return posts.Select(p =>
            {
                var hasAi =
                    p.AiLastCheckedAt.HasValue ||
                    p.FakePropertyEvaluation.HasValue ||
                    p.ImageManipulationEvaluation.HasValue ||
                    p.PostDocPathEvaluation != AIDecision.Uncertain ||
                    p.PriceEvaluation != PriceEvaluation.Acceptable;

                var needsAdmin =
                    p.PendingStatus == PostPendingStatus.Pending &&
                    p.PostDocPathEvaluation == AIDecision.Uncertain;

                return new PostSummaryDto
                {
                    PostId = p.PostId,
                    UserId = p.Landlord?.UserId ?? 0,
                    UserName = p.Landlord?.User?.UserName ?? "Unknown",

                    Title = p.Title,
                    Description = p.Description,
                    Price = (double)(p.Price ?? 0),
                    IsAuction = p.IsAuction,
                    DatePost = p.CreatedAt,
                    Images = p.PostImages?.Select(img => img.ImageUrl).ToList() ?? new List<string>(),

                    PendingStatus = p.PendingStatus,
                    Status = p.Status,
                    Type = p.Type,

                    PostDocPathEvaluation = p.PostDocPathEvaluation,
                    PriceEvaluation = p.PriceEvaluation,
                    FakePropertyEvaluation = p.FakePropertyEvaluation,
                    ImageManipulationEvaluation = p.ImageManipulationEvaluation,

                    AiConfidence = p.AiConfidence,
                    AiReason = p.AiReason,
                    AiLastCheckedAt = p.AiLastCheckedAt,

                    NeedsAdminReview = needsAdmin,
                    HasAiResults = hasAi
                };
            }).ToList();
        }

        public async Task CreatePostAsync(long landlordUserId, CreatePostDTO postDto)
        {
            var landlord = await GuardActiveLandlordAsync(landlordUserId);

            if (postDto.PostDocFile == null || postDto.PostDocFile.Length == 0)
                throw new ArgumentException("Post document file is required");

            if (!postDto.IsAuction)
            {
                if (!postDto.Price.HasValue || postDto.Price.Value <= 0)
                    throw new Exception("Price is required for non-auction posts.");
            }

            string savedDocPath = await _mediaService.SaveFileAsync(postDto.PostDocFile);

            var postImages = new List<PostImage>();
            if (postDto.Images != null && postDto.Images.Any())
            {
                foreach (var image in postDto.Images)
                {
                    string imagePath = await _mediaService.SaveFileAsync(image);
                    postImages.Add(new PostImage { ImageUrl = imagePath });
                }
            }

            var post = new Post
            {
                LandlordId = landlord.LandlordId,
                Title = postDto.Title,
                Description = postDto.Description,
                Price = postDto.Price,
                IsAuction = postDto.IsAuction,

                Location = postDto.Location,
                LocationPath = postDto.LocationPath,
                PostDocPath = savedDocPath,

                Status = PropertyStatus.Available,
                Type = postDto.Type,
                CreatedAt = DateTime.UtcNow,

                PendingStatus = PostPendingStatus.Pending,
                PostDocPathEvaluation = AIDecision.Uncertain,

                IsAdminFinalized = false,
                AdminFinalizedAtUtc = null,

                PostImages = postImages,

                NumberOfRooms = postDto.NumOfRooms,
                NumberOfBathrooms = postDto.NumOfBathrooms,
                Area = postDto.Area,
                TotalUnitsInBuilding = postDto.TotalUnitsInBuilding,
                IsFurnished = postDto.IsFurnished,
                HasGarage = postDto.HasGarage,
                FloorNumber = postDto.FloorNumber,
                StartRentalDate = postDto.StartRentalDate,
                EndRentalDate = postDto.EndRentalDate,

                TagsJson = postDto.Tags != null ? NormalizeTagsToJson(postDto.Tags) : null
            };

            await using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                await _postRepository.AddAsync(post);
                await _context.SaveChangesAsync();

                await EnqueuePostAiChecks(post);
                await EnqueuePostDocAiCheck(post);
                await EnqueueOwnerForecasts(post);

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                throw new Exception(ex.InnerException?.Message ?? ex.Message);
            }

            await _notificationService.SendNotificationAsync(
                userId: landlordUserId,
                title: "تم استلام المستندات",
                content: $"تم استلام مستندات ملكية عقار '{postDto.Title}' بنجاح، جاري فحصها من قبل الإدارة.",
                type: NotificationType.NewPost,
                targetUrl: $"/properties/{post.PostId}"
            );
        }

        public async Task Delete_Post(long landlordUserId, long postId)
        {
            var post = await GuardPostOwnedByLandlordAsync(landlordUserId, postId);

            if (post.Status == PropertyStatus.Sold)
                throw new Exception("Not allowed to delete a sold property.");

            _postRepository.Remove(post);
            await _postRepository.SaveChangesAsync();

            await _notificationService.SendNotificationAsync(
                userId: post.Landlord.UserId,
                        title: "تم إيقاف العقار",
                content: $"تم إيقاف ظهور عقارك {post.Title} بناءً على طلبك.",
                type: NotificationType.PostDeactivated
            );
        }

        public async Task Update_Post(long landlordUserId, long postId, UpdatePostDTO updateDto)
        {
            var post = await GuardPostOwnedByLandlordAsync(landlordUserId, postId);

            if (post.Status is PropertyStatus.UnderNegotiation or PropertyStatus.Sold)
                throw new Exception("Not allowed to edit a post that is under negotiation or sold.");

            post.PendingStatus = PostPendingStatus.Pending;
            post.IsAdminFinalized = false;
            post.AdminFinalizedAtUtc = null;

            post.FakePropertyEvaluation = null;
            post.ImageManipulationEvaluation = null;
            post.PriceEvaluation = PriceEvaluation.Acceptable;

            post.AiConfidence = null;
            post.AiReason = null;
            post.AiLastCheckedAt = null;

            post.PostImages ??= new List<PostImage>();

            if (!string.IsNullOrEmpty(updateDto.Title)) post.Title = updateDto.Title;
            if (!string.IsNullOrEmpty(updateDto.Description)) post.Description = updateDto.Description;

            if (!string.IsNullOrEmpty(updateDto.Location)) post.Location = updateDto.Location;
            if (!string.IsNullOrEmpty(updateDto.LocationPath)) post.LocationPath = updateDto.LocationPath;
            if (updateDto.RentalStatus.HasValue) post.Status = updateDto.RentalStatus.Value;

            if (updateDto.Tags != null)
                post.TagsJson = NormalizeTagsToJson(updateDto.Tags);

            await using var tx = await _context.Database.BeginTransactionAsync();

            try
            {
                _postRepository.Update(post);
                await _context.SaveChangesAsync();

                await EnqueuePostAiChecks(post);
                await EnqueueOwnerForecasts(post);

                await _context.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                throw new Exception(ex.InnerException?.Message ?? ex.Message);
            }

            await _notificationService.SendNotificationAsync(
            userId: post.Landlord.UserId,   
            title: "تم تعديل العقار",
            content: $"تم تعديل بيانات عقارك {post.Title} بناءً على طلبك, انتظر موافقة الادارة.",
            type: NotificationType.NewPost
);
        }

        // ============================================================
        // Proposals
        // ============================================================

        public async Task<IEnumerable<ProposalDto>> GetLandlordProposalsAsync(long landlordUserId)
        {
            var landlord = await GuardActiveLandlordAsync(landlordUserId);

            var postIds = await _context.Posts
                .Where(p => p.LandlordId == landlord.LandlordId)
                .Select(p => p.PostId)
                .ToListAsync();

            if (!postIds.Any())
                throw new InvalidOperationException("No posts found for this landlord.");

            var proposals = await _proposalRepository.NestedFind(
                p => postIds.Contains(p.PostId) && p.ProposalStatus != ProposalStatus.Rejected,
                p => p.User,
                p => p.Post,
                p => p.Post.Landlord,
                p => p.Post.Landlord.User,
                p => p.Post.PostImages
            );

            if (!proposals.Any())
                throw new KeyNotFoundException("No proposals found for your posts.");

            return proposals.Select(proposal => new ProposalDto
            {
                ProposalId = proposal.ProposalId,
                PostId = proposal.PostId,
                Title = proposal.Post?.Title,
                Description = proposal.Post?.Description,
                ImagePath = proposal.Post?.PostImages?.FirstOrDefault()?.ImageUrl,
                LandlordUserId = landlord.UserId,
                LandlordName = proposal.Post?.Landlord?.User?.UserName,
                TenantId = proposal.TenantId,
                TenantName = proposal.User?.UserName,
                Phone = proposal.Phone,
                StartRentalDate = proposal.StartRentalDate,
                EndRentalDate = proposal.EndRentalDate,
                ProposalStatus = proposal.ProposalStatus,
                IsInstallment = proposal.IsInstallment,
                FilePath = proposal.FilePath,
                OfferedPrice = proposal.Offeredprice ?? 0,

                PropertyType = proposal.Post!.Type,
                IsAuction = proposal.Post!.IsAuction,

            }).ToList();
        }

        public async Task<AcceptProposalResponseDto> AcceptProposal(long landlordUserId, long proposalId)
        {
            var proposal = await GuardProposalOwnedByLandlordAsync(landlordUserId, proposalId);

            if (proposal.ProposalStatus != ProposalStatus.Waiting)
                throw new Exception("You can only accept a waiting proposal.");

            var alreadyApproved = await _proposalRepository.FirstOrDefaultAsync(p =>
                p.PostId == proposal.PostId && p.ProposalStatus == ProposalStatus.Approved);

            if (alreadyApproved != null)
                throw new Exception("This post already has an approved proposal.");

            proposal.ProposalStatus = ProposalStatus.Approved;

            if (proposal.Post!.IsAuction)
            {
                if (!proposal.Offeredprice.HasValue || proposal.Offeredprice.Value <= 0)
                    throw new Exception("Winning proposal has invalid offered price.");

                proposal.Post.Price = proposal.Offeredprice.Value;
            }

            proposal.Post!.Status = PropertyStatus.UnderNegotiation;

            var others = await _proposalRepository.FindAsync(p =>
                p.PostId == proposal.PostId &&
                p.ProposalId != proposal.ProposalId &&
                p.ProposalStatus == ProposalStatus.Waiting);

            foreach (var p in others)
                p.ProposalStatus = ProposalStatus.Rejected;

            await _proposalRepository.SaveChangesAsync();

            var postType = proposal.Post.Type;
            var tenantId = proposal.TenantId;

            string? suggestedFlow;
            string? endpoint;

            if (postType == PropertyType.Sale)
            {
                if (proposal.IsInstallment == IsInstallment.Installment)
                {
                    suggestedFlow = "SALE_INSTALLMENT";
                    endpoint = $"/api/payments/sale/installment/{tenantId}";
                }
                else
                {
                    suggestedFlow = "SALE_CASH";
                    endpoint = $"/api/payments/sale/cash/{tenantId}";
                }
            }
            else
            {
                suggestedFlow = "RENT_START";
                endpoint = $"/api/payments/rent/start/{tenantId}";
            }

            await _notificationService.SendNotificationAsync(
                userId: proposal.TenantId,
                title: "تم قبول عرضك 🎉",
                content: "وافق المالك على عرضك! الخطوة التالية هي مراجعة وتوقيع العقد.",
                type: NotificationType.ProposalAccepted,
                targetUrl: "/contracts/{contractId}"
            );

            return new AcceptProposalResponseDto
            {
                ProposalId = proposal.ProposalId,
                PostId = proposal.PostId,
                TenantId = tenantId,
                LandlordUserId = landlordUserId,
                PropertyType = postType,
                IsAuction = proposal.Post.IsAuction,
                FinalPrice = proposal.Post.Price,
                NextAction = "INITIATE_PAYMENT_FLOW",
                SuggestedFlow = suggestedFlow,
                InitiateEndpoint = endpoint
            };
        }

        public async Task RejectProposal(long landlordUserId, long proposalId)
        {
            var proposal = await GuardProposalOwnedByLandlordAsync(landlordUserId, proposalId);

            proposal.ProposalStatus = ProposalStatus.Rejected;
            await _proposalRepository.SaveChangesAsync();

            await _notificationService.SendNotificationAsync(
                userId: proposal.TenantId,
                title: "تم رفض عرضك.",
                content: $"لم يوافق المالك على عرضك على عقار {proposal.Post.Title}!",
                type: NotificationType.ProposalAccepted,
                targetUrl: "/contracts/{contractId}"
            );

        }

        // ============================================================
        // AI Enqueue
        // ============================================================

        private async Task EnqueuePostAiChecks(Post post)
        {
            var payload = new
            {
                postId = post.PostId,
                landlordId = post.LandlordId,
                title = post.Title,
                description = post.Description,
                location = post.Location,
                locationPath = post.LocationPath,
                type = post.Type.ToString(),
                status = post.Status.ToString(),
                isAuction = post.IsAuction,
                price = post.Price,
                area = post.Area,
                numberOfRooms = post.NumberOfRooms,
                numberOfBathrooms = post.NumberOfBathrooms,
                floorNumber = post.FloorNumber,
                isFurnished = post.IsFurnished,
                hasGarage = post.HasGarage,
                startRentalDate = post.StartRentalDate,
                endRentalDate = post.EndRentalDate,
                tagsJson = post.TagsJson,
                images = post.PostImages?.Select(x => x.ImageUrl).ToList() ?? new List<string>()
            };

            await _aiRequestDispatcher.EnqueueAsync(AiRequestTypes.Fraud_FakePropertyDetection, "post", post.PostId, payload);
            await _aiRequestDispatcher.EnqueueAsync(AiRequestTypes.Price_AnomalyDetection, "post", post.PostId, payload);

            await _aiRequestDispatcher.EnqueueAsync(
                AiRequestTypes.Content_Moderation,
                "post",
                post.PostId,
                new { postId = post.PostId, title = post.Title, description = post.Description, tagsJson = post.TagsJson }
            );

            if (post.PostImages != null && post.PostImages.Any())
            {
                await _aiRequestDispatcher.EnqueueAsync(
                    AiRequestTypes.Fraud_ImageManipulation,
                    "post",
                    post.PostId,
                    new { postId = post.PostId, images = post.PostImages.Select(x => x.ImageUrl).ToList() }
                );

                await _aiRequestDispatcher.EnqueueAsync(
                    AiRequestTypes.Image_QualityScoring,
                    "post",
                    post.PostId,
                    new { postId = post.PostId, images = post.PostImages.Select(x => x.ImageUrl).ToList(), title = post.Title, location = post.Location }
                );
            }

            await _aiRequestDispatcher.EnqueueAsync(
                AiRequestTypes.Decision_Engine,
                "post",
                post.PostId,
                new
                {
                    postId = post.PostId,
                    title = post.Title,
                    description = post.Description,
                    location = post.Location,
                    type = post.Type.ToString(),
                    isAuction = post.IsAuction,
                    listedPrice = post.Price,
                    tagsJson = post.TagsJson,
                    hasImages = post.PostImages != null && post.PostImages.Any(),
                    imagesCount = post.PostImages?.Count ?? 0
                }
            );
        }

        private async Task EnqueuePostDocAiCheck(Post post)
        {
            await _aiRequestDispatcher.EnqueueAsync(
                AiRequestTypes.Fraud_PostDocumentAnalysis,
                "post",
                post.PostId,
                new { postId = post.PostId, postDocPath = post.PostDocPath, title = post.Title, location = post.Location }
            );
        }

        private async Task EnqueueOwnerForecasts(Post post)
        {
            var payload = new
            {
                postId = post.PostId,
                landlordId = post.LandlordId,
                title = post.Title,
                description = post.Description,
                location = post.Location,
                type = post.Type.ToString(),
                status = post.Status.ToString(),
                isAuction = post.IsAuction,
                price = post.Price,
                area = post.Area,
                numberOfRooms = post.NumberOfRooms,
                numberOfBathrooms = post.NumberOfBathrooms,
                floorNumber = post.FloorNumber,
                isFurnished = post.IsFurnished,
                hasGarage = post.HasGarage,
                startRentalDate = post.StartRentalDate,
                endRentalDate = post.EndRentalDate,
                tagsJson = post.TagsJson
            };

            await _aiRequestDispatcher.EnqueueAsync(AiRequestTypes.Owner_ForecastPrice, "post", post.PostId, payload);
            await _aiRequestDispatcher.EnqueueAsync(AiRequestTypes.Owner_ForecastDemand, "post", post.PostId, payload);
            await _aiRequestDispatcher.EnqueueAsync(AiRequestTypes.Owner_ForecastRevenue, "post", post.PostId, payload);
        }

        // ============================================================
        // Mapping + Tags
        // ============================================================

        private PostDTo MapToDTO(Post post, Landlord landlord)
        {
            var tags = ParseTagsFromJson(post.TagsJson);

            return new PostDTo
            {
                PostId = post.PostId,
                Title = post.Title,
                Description = post.Description,
                Price = (double)(post.Price ?? 0),
                Location = post.Location,
                LocationPath = post.LocationPath,

                DatePost = post.CreatedAt,
                StartRentalDate = post.StartRentalDate,
                EndRentalDate = post.EndRentalDate,

                FlagWaitingPost = post.PendingStatus,
                RentalStatus = post.Status,
                RentType = post.Type,
                PriceEvaluation = post.PriceEvaluation,
                PostDocPathEvaluation = post.PostDocPathEvaluation,

                NumOfRooms = post.NumberOfRooms,
                NumOfBathrooms = post.NumberOfBathrooms,
                Area = post.Area,
                TotalUnitsInBuilding = post.TotalUnitsInBuilding,
                IsFurnished = post.IsFurnished,
                HasGarage = post.HasGarage,
                FloorNumber = post.FloorNumber,

                PostDocPath = post.PostDocPath,
                Images = post.PostImages?.Select(pi => pi.ImageUrl).ToList() ?? new List<string>(),

                UserId = landlord.UserId,
                UserName = landlord.User?.UserName ?? "Unknown",

                Tags = tags,
                IsAuction = post.IsAuction
            };
        }

        private static string? NormalizeTagsToJson(List<string>? tags)
        {
            if (tags == null) return null;

            var cleaned = tags
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Select(t => t.Trim())
                .Where(t => t.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!cleaned.Any()) return null;
            return JsonSerializer.Serialize(cleaned);
        }

        private static List<string> ParseTagsFromJson(string? tagsJson)
        {
            if (string.IsNullOrWhiteSpace(tagsJson))
                return new List<string>();

            try
            {
                return JsonSerializer.Deserialize<List<string>>(tagsJson) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
