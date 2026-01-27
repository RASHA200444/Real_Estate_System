using System.Text.Json;
using otherServices.Models;
using otherServices.Models.DTOs;
using otherServices.Models.DTOs.Posts;
using otherServices.Models.Enums;
using otherServices.Repositories;

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

        public LandlordService(
            IWebHostEnvironment env,
            ILandlordRepository landlordRepository,
            IUserRepository userRepository,
            IPostRepository postRepository,
            IProposalRepository proposalRepository,
            IMediaService mediaService)
        {
            _env = env;
            _landlordRepository = landlordRepository;
            _userRepository = userRepository;
            _postRepository = postRepository;
            _proposalRepository = proposalRepository;
            _mediaService = mediaService;
        }

        #region Post

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

        public async Task<List<PostSummaryDto>> GetMyPostsAsync(long userId)
        {
            var posts = await _postRepository.NestedFind(
                p => p.Landlord.UserId == userId,
                p => p.Landlord,
                p => p.Landlord.User,
                p => p.PostImages
            );

            if (!posts.Any())
                throw new KeyNotFoundException("No posts found for this user.");

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

        public async Task CreatePostAsync(long userId, CreatePostDTO postDto)
        {
            var landlords = await _landlordRepository.NestedFind(
                l => l.UserId == userId,
                l => l.User
            );

            var landlord = landlords.FirstOrDefault();
            if (landlord == null) throw new KeyNotFoundException("Landlord not found");

            if (landlord.PendingStatus != PendingStatus.Active)
                throw new Exception("Landlord not active");

            if (postDto.PostDocFile == null || postDto.PostDocFile.Length == 0)
                throw new ArgumentException("Post document file is required");

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

            // ✅ Price rules:
            // - auction => price optional
            // - non-auction => price required (>0)
            if (!postDto.IsAuction)
            {
                if (!postDto.Price.HasValue || postDto.Price.Value <= 0)
                    throw new Exception("Price is required for non-auction posts.");
            }

            var post = new Post
            {
                LandlordId = landlord.LandlordId,
                Title = postDto.Title,
                Description = postDto.Description,
                Price = postDto.IsAuction ? postDto.Price : postDto.Price,  // both allowed but validated above
                IsAuction = postDto.IsAuction,

                Location = postDto.Location,
                LocationPath = postDto.LocationPath,
                PostDocPath = savedDocPath,

                Status = PropertyStatus.Available,
                Type = postDto.Type,
                CreatedAt = DateTime.UtcNow,
                PendingStatus = PostPendingStatus.Pending,

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

            await _postRepository.AddAsync(post);
            await _postRepository.SaveChangesAsync();
        }

        public async Task Delete_Post(long postId)
        {
            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null) throw new KeyNotFoundException("Post not found");

            if (post.Status == PropertyStatus.Sold)
                throw new Exception("Not Allowed to delete a Sold Property");

            _postRepository.Remove(post);
            await _postRepository.SaveChangesAsync();
        }

        public async Task Update_Post(long postId, UpdatePostDTO updateDto)
        {
            var posts = await _postRepository.NestedFind(
                p => p.PostId == postId,
                p => p.PostImages
            );

            var post = posts.FirstOrDefault();
            if (post == null) throw new KeyNotFoundException("Post not found");



            post.PendingStatus = PostPendingStatus.Pending;

            post.PostImages ??= new List<PostImage>();

            if (!string.IsNullOrEmpty(updateDto.Title)) post.Title = updateDto.Title;
            if (!string.IsNullOrEmpty(updateDto.Description)) post.Description = updateDto.Description;

            // ✅ Price is NOT editable after creation (your rule)
            //if (updateDto.Price.HasValue)
            //    throw new Exception("Price cannot be updated after post creation.");

            if (!string.IsNullOrEmpty(updateDto.Location)) post.Location = updateDto.Location;
            if (!string.IsNullOrEmpty(updateDto.LocationPath)) post.LocationPath = updateDto.LocationPath;
            if (updateDto.RentalStatus.HasValue) post.Status = updateDto.RentalStatus.Value;

            if (updateDto.Tags != null)
                post.TagsJson = NormalizeTagsToJson(updateDto.Tags);

            _postRepository.Update(post);
            await _postRepository.SaveChangesAsync();
        }

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

                Tags = tags
            };
        }

        #endregion

        #region Helpers
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
        #endregion

        #region Proposals

        public async Task AcceptProposal(long proposalId)
        {
            var proposals = await _proposalRepository.NestedFind(
                p => p.ProposalId == proposalId,
                p => p.Post
            );

            var proposal = proposals.FirstOrDefault();
            if (proposal == null)
                throw new KeyNotFoundException("Proposal not found");

            if (proposal.Post == null)
                throw new Exception("Post not found");

            // ✅ (B) لازم تكون Waiting
            if (proposal.ProposalStatus != ProposalStatus.Waiting)
                throw new Exception("You can only accept a waiting proposal.");

            // ✅ ممنوع accept لو فيه Approved قبل كده
            var alreadyApproved = await _proposalRepository.FirstOrDefaultAsync(p =>
                p.PostId == proposal.PostId &&
                p.ProposalStatus == ProposalStatus.Approved);

            if (alreadyApproved != null)
                throw new Exception("This post already has an approved proposal.");

            // ✅ approve winner
            proposal.ProposalStatus = ProposalStatus.Approved;

            // ✅ لو مزاد: السعر النهائي يبقى سعر البروپوزال الفائز
            if (proposal.Post.IsAuction)
            {
                if (!proposal.Offeredprice.HasValue || proposal.Offeredprice.Value <= 0)
                    throw new Exception("Winning proposal has invalid offered price.");

                // post.Price nullable عندك
                proposal.Post.Price = proposal.Offeredprice.Value;
            }

            // ✅ حالة البوست
            proposal.Post.Status = PropertyStatus.UnderNegotiation;

            // ✅ اقفل باقي الـ Waiting proposals
            var others = await _proposalRepository.FindAsync(p =>
                p.PostId == proposal.PostId &&
                p.ProposalId != proposal.ProposalId &&
                p.ProposalStatus == ProposalStatus.Waiting);

            foreach (var p in others)
                p.ProposalStatus = ProposalStatus.Rejected;

            await _proposalRepository.SaveChangesAsync();
        }


        public async Task RejectProposal(long proposalId)
        {
            var proposal = await _proposalRepository.GetByIdAsync(proposalId);
            if (proposal == null) throw new KeyNotFoundException("Proposal not found");

            proposal.ProposalStatus = ProposalStatus.Rejected;
            await _proposalRepository.SaveChangesAsync();
        }

        public async Task<IEnumerable<ProposalDto>> GetLandlordProposalsAsync(long userId)
        {
            var landlord = (await _landlordRepository.NestedFind(
                l => l.UserId == userId,
                l => l.User,
                l => l.Posts
            )).FirstOrDefault();

            if (landlord == null)
                throw new KeyNotFoundException("Landlord not found for this user.");

            if (!landlord.Posts.Any())
                throw new InvalidOperationException("No posts found for this landlord.");

            var postIds = landlord.Posts.Select(p => p.PostId).ToList();

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
                OfferedPrice = proposal.Offeredprice ?? 0
            }).ToList();
        }

        #endregion
    }
}
