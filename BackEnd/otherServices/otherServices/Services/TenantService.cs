using CommentAPI.DTOs;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using otherServices.Models;
using otherServices.Models.DTOs;
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



        public TenantService(AppDbContext2 context , IMediaService mediaService, IWebHostEnvironment env, IProposalRepository proposalRepository, IPostRepository postRepository, ISavedPostRepository savedPostRepository , IUserRepository userRepository)
        {
            _env = env;
            _proposalRepository = proposalRepository;
            _postRepository = postRepository;
            _savedPostRepository = savedPostRepository;
            _userRepository = userRepository;
            _mediaService = mediaService;
            _context = context;
        }


        public async Task<IEnumerable<PostDTo>> GetPosts()
        {
            var posts = await _postRepository.NestedFind(
                                p => p.PendingStatus == PostPendingStatus.Accepted,
                                p => p.Landlord,
                                p => p.Landlord.User,
                                p => p.PostImages
                            );


            return posts.Select(p =>
            {
                return new PostDTo
                {
                    PostId = p.PostId,
                    Title = p.Title,
                    Description = p.Description,
                    Price = p.Price,
                    PriceEvaluation = p.PriceEvaluation,
                    Location = p.Location,
                    LocationPath = p.LocationPath,
                    RentalStatus = p.Status,
                    DatePost = p.CreatedAt,
                    FlagWaitingPost = p.PendingStatus,

                    UserId = p.Landlord?.UserId ?? 0,
                    LandlordId = p.Landlord?.LandlordId ?? 0,
                    UserName = p.Landlord?.User?.UserName ?? "Unknown",
                    Email = p.Landlord?.User?.Email ?? "Unknown",

                    Images = p.PostImages?.Select(img => img.ImageUrl).ToList()
                             ?? new List<string>()  
                };
            }).ToList();
        }


        public async Task<List<SavedPostDto>> GetMySavedPosts(long userId)
        {
            var savedPosts = await _savedPostRepository.NestedFind(
                sp => sp.UserId == userId,
                sp => sp.Post,
                sp => sp.Post.Landlord,
                sp => sp.Post.Landlord.User,  
                sp => sp.Post.PostImages,
                sp => sp.Post.Comments);

            if (!savedPosts.Any())
                throw new KeyNotFoundException("No saved posts found.");

            var result = new List<SavedPostDto>();

            foreach (var sp in savedPosts)
            {
                var post = sp.Post;

                result.Add(new SavedPostDto
                {
                    PostId = post.PostId,
                    Title = post.Title,
                    Description = post.Description,
                    Price = post.Price,
                    Location = post.Location,
                    CreatedAt = post.CreatedAt,
                    RentalStatus = post.Status,
                    FlagWaitingPost = post.PendingStatus,
                    PostDocPath = post.PostDocPath,

                    landlordId = post.Landlord?.UserId ?? 0,
                    landlordUserName = post.Landlord?.User?.UserName ?? "Unknown",

                    Images = post.PostImages?.Select(pi => pi.ImageUrl).ToList(),

                    //Comments = post.Comments?.Select(c => new PostsCommentsDto
                    //{
                    //    CommentId = c.CommentId,
                    //    PostId = c.PostId,
                    //    Comment_Written = c.Description,
                    //    CreatedAt = c.CreatedAt
                    //}).ToList()
                });
            }

            return result;
        }

        //public async Task<List<SavedPostDto>> GetMySavedPosts(long userId)
        //{
        //    var savedPosts = await _savedPostRepository.NestedFind(
        //        sp => sp.UserId == userId,
        //        sp => sp.Post,
        //        sp => sp.Post.Landlord,
        //        sp => sp.Post.Landlord.User, 
        //        sp => sp.Post.PostImages,
        //        sp => sp.Post.Comments);

        //    if (!savedPosts.Any()) throw new KeyNotFoundException("No saved posts found.");

        //    var result = new List<SavedPostDto>();

        //    foreach (var sp in savedPosts)
        //    {
        //        var post = sp.Post;

        //        result.Add(new SavedPostDto
        //        {
        //            PostId = post.PostId,
        //            Title = post.Title,
        //            Description = post.Description,
        //            Price = post.Price,
        //            Location = post.Location,
        //            CreatedAt = post.CreatedAt,
        //            RentalStatus = post.Status,
        //            FlagWaitingPost = post.PendingStatus,
        //            PostDocPath = post.PostDocPath,

        //            landlordId = post.Landlord?.UserId ?? 0,
        //            landlordUserName = post.Landlord?.User?.UserName ?? "Unknown",

        //            //Comments = post.Comments.Select(c => new PostsCommentsDto
        //            //{
        //            //    CommentId = c.CommentId,
        //            //    PostId = c.PostId,
        //            //    Comment_Written = c.Description,
        //            //    CreatedAt = c.CreatedAt
        //            //}).ToList()
        //        });
        //    }

        //    return result;
        //}

        public async Task<bool> Save_Post(long userId, long postId)
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

            return true;
        }
        public async Task<bool> cancelSave(long userId, long postId)
        {
            var post = (await _savedPostRepository.FindAsync(sp => sp.UserId == userId && sp.PostId == postId)).FirstOrDefault();
            if (post == null) return false;

            _savedPostRepository.Remove(post);
            await _savedPostRepository.SaveChangesAsync();
            return true;
        }



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

            foreach (var proposal in proposals)
            {
                Console.WriteLine($"Proposal {proposal.ProposalId}");
                Console.WriteLine($"Post: {(proposal.Post != null ? "Exists" : "Null")}");
                Console.WriteLine($"Landlord: {(proposal.Post?.Landlord != null ? "Exists" : "Null")}");
                Console.WriteLine($"User: {(proposal.Post?.Landlord?.User != null ? "Exists" : "Null")}");
                Console.WriteLine($"PostImages count: {proposal.Post?.PostImages?.Count ?? 0}");
            }



            return await Task.WhenAll(proposals.Select(async proposal =>
            {
                var post = proposal.Post;
                var landlord = post?.Landlord;
                var landlordUser = landlord?.User;

                return new ProposalDto
                {
                    ProposalId = proposal.ProposalId,
                    PostId = proposal.PostId,
                    Title = post?.Title ?? "N/A",
                    ImagePath = post?.PostImages?.FirstOrDefault()?.ImageUrl ?? string.Empty,
                    TenantId = proposal.TenantId,
                    TenantName = proposal.User.UserName,
                    Phone = proposal.Phone,
                    StartRentalDate = proposal.StartRentalDate,
                    EndRentalDate = proposal.EndRentalDate,
                    ProposalStatus = proposal.ProposalStatus,
                    IsInstallment = proposal.IsInstallment,
                    FilePath = proposal.FilePath,
                    OfferedPrice = proposal.Offeredprice,

                    LandlordId = landlordUser?.Landlord.LandlordId ?? 0,
                    LandlordUserId = landlordUser?.UserId ?? 0,
                    LandlordName = landlordUser?.UserName ?? "Unknown"
                };
            }));
            
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

        public async Task<LandlordDto> UpgradeToLandlord(long userId, LandlordUpgradeRequestDto dto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId);

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

            return new LandlordDto
            {
                UserId = user.UserId,
                LandlordId = landlord.LandlordId,

                UserName = user.UserName,
                Email = user.Email,
                RoleName = user.RoleName.ToString(),

                ProfilePhotoPath = user.ProfilePhotoPath,
                NIDPath = user.NIDPath,
                NIDEvaluation = (int)user.NIDEvaluation,

                OwnershipDocPath = landlord.OwnershipDocPath,
                OwnershipDocPathEvaluation = (int)landlord.OwnershipDocPathEvaluation,

                PendingStatus = (int)landlord.PendingStatus,
                IsPro = landlord.IsPro,
                ComPanStatus = (int)landlord.ComPanStatus,

                Rate = (int)landlord.Rate
            };
        }
    }


}
