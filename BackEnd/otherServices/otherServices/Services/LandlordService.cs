using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using otherServices.Models;
using otherServices.Models.DTOs;
using otherServices.Repositories;
using otherServices.Models.Enums;
using WebAPIDotNet.DTOs;
using WebAPIDotNet.Services;

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

        public async Task<PostDTo> Get_Post_By_Id(int postId)
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

        public async Task<List<PostDTo>> Get_Posts_By_User(int userId)
        {
            var posts = await _postRepository.NestedFind(
                p => p.Landlord.UserId == userId,
                p => p.Landlord,
                p => p.Landlord.User,
                p => p.PostImages
            );

            return posts.Select(p => MapToDTO(p, p.Landlord)).ToList();
        }

        public async Task<PostDTo> Create_Post(int userId, CreatePostDTO postDto)
        {
            var landlords = await _landlordRepository.NestedFind(
                l => l.UserId == userId,
                l => l.User
            );

            var landlord = landlords.FirstOrDefault();
            if (landlord == null)
                throw new KeyNotFoundException("Landlord not found");

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

            // ✅ Tags -> JSON string stored in Post.TagsJson
            string? tagsJson = NormalizeTagsToJson(postDto.Tags);

            var post = new Post
            {
                LandlordId = landlord.LandlordId,
                Title = postDto.Title,
                Description = postDto.Description,
                Price = postDto.Price,
                Location = postDto.Location,
                LocationPath = postDto.LocationPath,
                PostDocPath = savedDocPath,

                Status = PropertyStatus.Available,
                Type = postDto.Type,
                CreatedAt = DateTime.Now,
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

                // ✅ NEW
                TagsJson = tagsJson
            };

            try
            {
                await _postRepository.AddAsync(post);
                await _postRepository.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.InnerException?.Message ?? ex.Message);
            }

            return MapToDTO(post, landlord);
        }

        public async Task<bool> Delete_Post(long postId)
        {
            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null) return false;

            _postRepository.Remove(post);
            await _postRepository.SaveChangesAsync();
            return true;
        }

        public async Task<PostDTo> Update_Post(long postId, UpdatePostDTO updateDto)
        {
            var posts = await _postRepository.NestedFind(
                p => p.PostId == postId,
                p => p.PostImages
            );

            var post = posts.FirstOrDefault();
            if (post == null)
                throw new KeyNotFoundException("Post not found");

            post.PostImages ??= new List<PostImage>();

            if (!string.IsNullOrEmpty(updateDto.Title)) post.Title = updateDto.Title;
            if (!string.IsNullOrEmpty(updateDto.Description)) post.Description = updateDto.Description;
            if (updateDto.Price.HasValue) post.Price = updateDto.Price.Value;
            if (!string.IsNullOrEmpty(updateDto.Location)) post.Location = updateDto.Location;
            if (!string.IsNullOrEmpty(updateDto.LocationPath)) post.LocationPath = updateDto.LocationPath;
            if (updateDto.RentalStatus.HasValue) post.Status = updateDto.RentalStatus.Value;

            // ✅ NEW: update tags if provided
            if (updateDto.Tags != null)
            {
                post.TagsJson = NormalizeTagsToJson(updateDto.Tags);
            }

            _postRepository.Update(post);
            await _postRepository.SaveChangesAsync();

            var landlord = await _landlordRepository.NestedFind(
                l => l.LandlordId == post.LandlordId,
                l => l.User
            );

            var landlordEntity = landlord.FirstOrDefault();
            if (landlordEntity == null || landlordEntity.User == null)
                throw new Exception("Landlord or User data is missing");

            return MapToDTO(post, landlordEntity);
        }

        private PostDTo MapToDTO(Post post, Landlord landlord)
        {
            var tags = ParseTagsFromJson(post.TagsJson);

            return new PostDTo
            {
                PostId = post.PostId,
                Title = post.Title,
                Description = post.Description,
                Price = post.Price,
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
                Email = landlord.User?.Email ?? "Unknown",

                // ✅ NEW
                Tags = tags
            };
        }

        // =========================
        // Helpers for Tags JSON
        // =========================

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

        // =========================
        // Proposals
        // =========================

        public async Task<Proposal> AcceptProposal(long proposalId)
        {
            var proposals = await _proposalRepository.NestedFind(p => p.ProposalId == proposalId, p => p.Post);
            var proposal = proposals.FirstOrDefault();
            if (proposal == null)
                throw new KeyNotFoundException("Proposal not found");

            proposal.ProposalStatus = ProposalStatus.Approved;
            if (proposal.Post != null)
                proposal.Post.Status = PropertyStatus.Sold;

            await _proposalRepository.SaveChangesAsync();
            return proposal;
        }

        public async Task<Proposal> RejectProposal(long proposalId)
        {
            var proposal = await _proposalRepository.GetByIdAsync(proposalId);
            if (proposal == null) throw new KeyNotFoundException("Proposal not found");

            proposal.ProposalStatus = ProposalStatus.Rejected;
            await _proposalRepository.SaveChangesAsync();
            return proposal;
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
                p => postIds.Contains(p.PostId)
                     && p.ProposalStatus != ProposalStatus.Rejected,
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
                ImagePath = proposal.Post?.PostImages?.FirstOrDefault()?.ImageUrl,

                LandlordId = proposal.Post?.LandlordId ?? 0,
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
                OfferedPrice = proposal.Offeredprice
            }).ToList();
        }
    }
}
