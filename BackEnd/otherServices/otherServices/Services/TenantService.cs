using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using otherServices.Models;
using otherServices.Models.DTOs;
using otherServices.Models.DTOs.Posts;
using otherServices.Models.Enums;
using otherServices.Repositories;
using WebAPIDotNet.DTOs;
using WebAPIDotNet.Services;
using CommentAPI.DTOs;

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

        public TenantService(
            AppDbContext2 context,
            IMediaService mediaService,
            IWebHostEnvironment env,
            IProposalRepository proposalRepository,
            IPostRepository postRepository,
            ISavedPostRepository savedPostRepository,
            IUserRepository userRepository)
        {
            _env = env;
            _proposalRepository = proposalRepository;
            _postRepository = postRepository;
            _savedPostRepository = savedPostRepository;
            _userRepository = userRepository;
            _mediaService = mediaService;
            _context = context;
        }

        // =========================
        // Posts (Tenant browse)
        // =========================
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
                Price = p.Price,

                DatePost = p.CreatedAt,

                Images = p.PostImages?
                            .Select(img => img.ImageUrl)
                            .ToList()
                         ?? new List<string>()
            }).ToList();
        }

        // =========================
        // Saved Posts
        // =========================
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
                    Price = post.Price,

                    DatePost = post.CreatedAt,

                    Images = post.PostImages?
                                .Select(pi => pi.ImageUrl)
                                .ToList()
                             ?? new List<string>()
                };
            }).ToList();
        }

        public async Task Save_Post(long userId, long postId)
        {
            var user = await _userRepository.GetByIdAsync(userId);
            if (user == null)
                throw new Exception("User not found");

            var post = await _postRepository.GetByIdAsync(postId);
            if (post == null)
                throw new Exception("Post not found");

            var exists = (await _savedPostRepository.FindAsync(sp => sp.UserId == userId && sp.PostId == postId)).Any();
            if (exists) throw new Exception("Post is already Saved");

            await _savedPostRepository.AddAsync(new SavedPost
            {
                UserId = userId,
                PostId = postId,
            });

            await _savedPostRepository.SaveChangesAsync();
        }

        public async Task cancelSave(long userId, long postId)
        {
            var post = (await _savedPostRepository.FindAsync(sp => sp.UserId == userId && sp.PostId == postId)).FirstOrDefault();
            if (post == null) throw new Exception("Post not found in Your Saves");

            _savedPostRepository.Remove(post);
            await _savedPostRepository.SaveChangesAsync();
        }

        // =========================
        // Proposals
        // =========================
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
                throw new KeyNotFoundException("No proposals found for this tenant.");

            if (proposals.Any(p => p.Post == null))
                throw new InvalidOperationException("One or more proposals are linked to a missing post.");

            if (proposals.Any(p => p.Post!.Landlord == null))
                throw new InvalidOperationException("One or more proposals are linked to a missing landlord.");

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
                    ImagePath = post.PostImages?.FirstOrDefault()?.ImageUrl ?? string.Empty,

                    TenantId = proposal.TenantId,
                    TenantName = proposal.User?.UserName ?? "Unknown",
                    Phone = proposal.Phone,
                    StartRentalDate = proposal.StartRentalDate,
                    EndRentalDate = proposal.EndRentalDate,
                    ProposalStatus = proposal.ProposalStatus,
                    IsInstallment = proposal.IsInstallment,
                    FilePath = proposal.FilePath,
                    OfferedPrice = proposal.Offeredprice,

                    LandlordId = landlord.LandlordId,
                    LandlordUserId = landlordUser?.UserId ?? 0,
                    LandlordName = landlordUser?.UserName ?? "Unknown"
                };
            }).ToList();
        }

        public async Task<ProposalDto> SubmitProposalAsync(long TenantId, long PostId, SubmitProposalDto form)
        {
            var posts = await _postRepository.NestedFind(
                p => p.PostId == PostId,
                p => p.Landlord,
                p => p.Landlord.User,
                p => p.PostImages
            );

            var post = posts.FirstOrDefault();
            if (post == null)
                throw new KeyNotFoundException("Post not found");

            // ✅ constraints (VERY IMPORTANT)
            if (post.PendingStatus != PostPendingStatus.Accepted)
                throw new Exception("Post is not approved by admin.");

            if (post.Status != PropertyStatus.Available)
                throw new Exception("Post is not available.");

            var existingProposal = await _proposalRepository.FirstOrDefaultAsync(p =>
                p.PostId == PostId &&
                p.TenantId == TenantId &&
                p.ProposalStatus == ProposalStatus.Waiting);

            if (existingProposal != null)
                throw new InvalidOperationException("You already have a pending proposal for this post. You cannot submit another until its status changes.");

            if (post.Type == PropertyType.Rent)
            {
                if (form.StartRentalDate == default || form.EndRentalDate == default)
                    throw new ArgumentException("Rental dates are required for rent properties.");

                if (form.IsInstallment != IsInstallment.Cash)
                    throw new ArgumentException("Installment is not allowed for rent properties.");
            }
            else if (post.Type == PropertyType.Sale)
            {
                if (form.StartRentalDate != default || form.EndRentalDate != default)
                    throw new ArgumentException("Rental dates are not allowed for sale properties.");
            }

            string? FilePath = await _mediaService.SaveFileAsync(form.File);
            if (string.IsNullOrEmpty(FilePath))
                throw new Exception("File saving failed");

            var proposal = new Proposal
            {
                PostId = PostId,
                TenantId = TenantId,
                Phone = form.Phone,
                StartRentalDate = post.Type == PropertyType.Rent ? form.StartRentalDate : null,
                EndRentalDate = post.Type == PropertyType.Rent ? form.EndRentalDate : null,
                IsInstallment = post.Type == PropertyType.Sale ? form.IsInstallment : IsInstallment.Cash,
                Offeredprice = form.Offeredprice,
                FilePath = FilePath,
                ProposalStatus = ProposalStatus.Waiting
            };

            await _proposalRepository.AddAsync(proposal);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                throw new Exception(errorMessage);
            }

            return new ProposalDto
            {
                ProposalId = proposal.ProposalId,
                PostId = proposal.PostId,
                Title = post.Title,
                ImagePath = post.PostImages.FirstOrDefault()?.ImageUrl,
                LandlordId = post.Landlord.LandlordId,
                LandlordUserId = post.Landlord.UserId,
                LandlordName = post.Landlord.User?.UserName,
                TenantId = TenantId,
                TenantName = null,
                Phone = proposal.Phone,
                StartRentalDate = proposal.StartRentalDate,
                EndRentalDate = proposal.EndRentalDate,
                ProposalStatus = proposal.ProposalStatus,
                IsInstallment = proposal.IsInstallment,
                FilePath = FilePath,
                OfferedPrice = proposal.Offeredprice,
            };
        }

        public async Task<bool> DeleteProposalAsync(long proposalId)
        {
            var proposal = await _proposalRepository.GetByIdAsync(proposalId);
            if (proposal == null) return false;

            if (!string.IsNullOrEmpty(proposal.FilePath) && File.Exists(proposal.FilePath))
                File.Delete(proposal.FilePath);

            _proposalRepository.Remove(proposal);
            await _proposalRepository.SaveChangesAsync();
            return true;
        }

        public async Task<ProposalDto> EditProposalAsync(long proposalId, ProposalEditDto updated)
        {
            var proposal = await _proposalRepository.GetByIdAsync(proposalId);
            if (proposal == null)
                throw new KeyNotFoundException("Proposal not found");

            var post = await _postRepository.GetByIdAsync(proposal.PostId);
            if (post == null)
                throw new KeyNotFoundException("Post not found");

            if (post.Type == PropertyType.Rent)
            {
                if ((updated.StartRentalDate.HasValue && !updated.EndRentalDate.HasValue) ||
                    (!updated.StartRentalDate.HasValue && updated.EndRentalDate.HasValue))
                {
                    throw new ArgumentException("Both StartRentalDate and EndRentalDate are required for rent properties if one is provided.");
                }

                if (updated.IsInstallment.HasValue && updated.IsInstallment != IsInstallment.Cash)
                    throw new ArgumentException("Installment is not allowed for rent properties.");
            }
            else if (post.Type == PropertyType.Sale)
            {
                if ((updated.StartRentalDate.HasValue || updated.EndRentalDate.HasValue))
                    throw new ArgumentException("Rental dates are not allowed for sale properties.");
            }

            if (updated.Offeredprice.HasValue && updated.Offeredprice <= 0)
                throw new ArgumentException("Offered price must be greater than zero.");

            if (!string.IsNullOrEmpty(updated.Phone))
                proposal.Phone = updated.Phone;

            if (updated.StartRentalDate.HasValue)
                proposal.StartRentalDate = post.Type == PropertyType.Rent ? updated.StartRentalDate : null;

            if (updated.EndRentalDate.HasValue)
                proposal.EndRentalDate = post.Type == PropertyType.Rent ? updated.EndRentalDate : null;

            if (updated.IsInstallment.HasValue)
                proposal.IsInstallment = post.Type == PropertyType.Sale ? updated.IsInstallment.Value : IsInstallment.Cash;

            if (updated.Offeredprice.HasValue)
                proposal.Offeredprice = updated.Offeredprice.Value;

            if (updated.File != null && updated.File.Length > 0)
            {
                if (!string.IsNullOrEmpty(proposal.FilePath) && File.Exists(proposal.FilePath))
                    File.Delete(proposal.FilePath);

                string filePath = await _mediaService.SaveFileAsync(updated.File);
                if (string.IsNullOrEmpty(filePath))
                    throw new Exception("File saving failed");

                proposal.FilePath = filePath;
            }

            await _proposalRepository.SaveChangesAsync();

            return new ProposalDto
            {
                ProposalId = proposal.ProposalId,
                PostId = proposal.PostId,
                Title = post?.Title ?? string.Empty,
                ImagePath = post?.PostImages?.FirstOrDefault()?.ImageUrl ?? string.Empty,
                LandlordId = post?.Landlord?.LandlordId ?? 0,
                LandlordUserId = post?.Landlord?.UserId ?? 0,
                LandlordName = post?.Landlord?.User?.UserName,
                TenantId = proposal.TenantId,
                TenantName = null,
                Phone = proposal.Phone,
                StartRentalDate = proposal.StartRentalDate,
                EndRentalDate = proposal.EndRentalDate,
                ProposalStatus = proposal.ProposalStatus,
                IsInstallment = proposal.IsInstallment,
                OfferedPrice = proposal.Offeredprice,
                FilePath = proposal.FilePath
            };
        }

        public async Task UpgradeToLandlord(long userId, LandlordUpgradeRequestDto dto)
        {
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userId);
            if (user == null)
                throw new Exception("User not found");

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

            await _context.SaveChangesAsync();
        }

        // =========================
        // Helpers
        // =========================
        private static List<string> ParseTagsJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<string>();
            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }
    }
}
