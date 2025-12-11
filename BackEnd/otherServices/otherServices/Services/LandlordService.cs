using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using otherServices.Models;
using otherServices.Models.DTOs;
using WebAPIDotNet.DTOs;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;
using Microsoft.Extensions.Hosting;
using otherServices.Repositories;
using otherServices.Models.Enums;
using WebAPIDotNet.Services;
using Microsoft.AspNetCore.Http.HttpResults;

namespace otherServices.Services
{
    public class LandlordService : ILandlordService
    {
        private readonly IWebHostEnvironment _env;
        private readonly ILandlordRepository _userRepository;
        private readonly IPostRepository _postRepository;
        private readonly IProposalRepository _proposalRepository;
        private readonly IMediaService _mediaService;
        public LandlordService(IWebHostEnvironment env, ILandlordRepository userRepository, IPostRepository postRepository, IProposalRepository proposalRepository, IMediaService mediaService)
        {
            _env = env;
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
        public async Task<List<PostDTo>> Get_Posts_By_LandlordId(int landlordId)
        {
            var posts = await _postRepository.NestedFind(
                p => p.LandlordId == landlordId, 
                p => p.Landlord,
                p => p.Landlord.User,
                p => p.PostImages

            );
            return posts.Select(p => MapToDTO(p, p.Landlord)).ToList();
        }
        public async Task<PostDTo> Create_Post(int landlordId, CreatePostDTO postDto)
        {
            var landlords = await _userRepository.NestedFind(
                l => l.LandlordId == landlordId,
                l => l.User
            );

            var landlord = landlords.FirstOrDefault();

            if (landlord == null)
                throw new KeyNotFoundException("Landlord not found");

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

            var post = new Post
            {
                LandlordId = landlordId,
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
                EndRentalDate = postDto.EndRentalDate
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

            // 🟢 تحديث الحقول الأساسية
            if (!string.IsNullOrEmpty(updateDto.Title)) post.Title = updateDto.Title;
            if (!string.IsNullOrEmpty(updateDto.Description)) post.Description = updateDto.Description;
            if (updateDto.Price.HasValue) post.Price = updateDto.Price.Value;
            if (!string.IsNullOrEmpty(updateDto.Location)) post.Location = updateDto.Location;
            if (!string.IsNullOrEmpty(updateDto.LocationPath)) post.LocationPath = updateDto.LocationPath;
            if (updateDto.RentalStatus.HasValue) post.Status = updateDto.RentalStatus.Value;



            //// update PostDocFile
            //if (updateDto.PostDocFile != null)
            //{
            //    if (!string.IsNullOrEmpty(post.PostDocPath))
            //    {
            //        string oldPath = Path.Combine(Directory.GetCurrentDirectory(), post.PostDocPath.TrimStart('\\', '/'));
            //        if (File.Exists(oldPath))
            //            File.Delete(oldPath);
            //    }

            //    string newDocPath = await _mediaService.SaveFileAsync(updateDto.PostDocFile);
            //    post.PostDocPath = newDocPath;
            //    post.PostDocPathEvaluation = AIDecision.NotReviewed;
            //}

            // // delete images
            //if (updateDto.ImagesToDelete != null && updateDto.ImagesToDelete.Any())
            //{
            //    var imagesToDelete = post.PostImages
            //        .Where(pi =>
            //            updateDto.ImagesToDelete.Any(d =>
            //                pi.ImageUrl.EndsWith(d, StringComparison.OrdinalIgnoreCase)))
            //        .ToList();

            //    foreach (var img in imagesToDelete)
            //    {
            //        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), img.ImageUrl.TrimStart('\\', '/'));
            //        if (File.Exists(fullPath))
            //            File.Delete(fullPath);

            //        post.PostImages.Remove(img);
            //    }
            //}

            //// new images
            //if (updateDto.NewImages != null)
            //{
            //    foreach (var file in updateDto.NewImages.Where(f => f != null))
            //    {
            //        string imagePath = await _mediaService.SaveFileAsync(file);
            //        post.PostImages.Add(new PostImage { ImageUrl = imagePath });
            //    }
            //}

            _postRepository.Update(post);
            await _postRepository.SaveChangesAsync();

            var landlord = await _userRepository.NestedFind(
                                l => l.LandlordId == post.LandlordId,
                                l => l.User
                                );
            var landlordEntity = landlord.FirstOrDefault();

            if (landlordEntity == null || landlordEntity.User == null)
                throw new Exception("Landlord or User data is missing");
            if (landlordEntity == null)
                Console.WriteLine("Landlord is null");
            if (landlordEntity?.User == null)
                Console.WriteLine("User is null");


            try
            {
                return MapToDTO(post, landlordEntity);
            }
            catch (Exception ex)
            {
                Console.WriteLine("MapToDTO error: " + ex.Message);
                throw;
            }
        }









        #region rfffffffffffff
        //public async Task<PostDTo> Update_Post(long postId, UpdatePostDTO updateDto)
        //{
        //    var posts = await _postRepository.NestedFind(
        //                                        p => p.PostId == postId,
        //                                        p => p.PostImages);
        //    var post = posts.FirstOrDefault();

        //    if (post == null)
        //        throw new KeyNotFoundException("Post not found");

        //    // 🛡 تأكد أن PostImages ليس null
        //    post.PostImages ??= new List<PostImage>();

        //    if (!string.IsNullOrEmpty(updateDto.Title)) post.Title = updateDto.Title;
        //    if (!string.IsNullOrEmpty(updateDto.Description)) post.Description = updateDto.Description;
        //    if (updateDto.Price.HasValue) 
        //        {
        //            post.Price = updateDto.Price.Value;
        //            post.PriceEvaluation = PriceEvaluation.Acceptable;
        //        }
        //    if (!string.IsNullOrEmpty(updateDto.Location)) post.Location = updateDto.Location;
        //    if (!string.IsNullOrEmpty(updateDto.LocationPath)) post.LocationPath = updateDto.LocationPath;
        //    if (updateDto.RentalStatus.HasValue) post.Status = updateDto.RentalStatus.Value;

        //    // 🟢 تحديث مستند العقار لو اتبعت ملف جديد
        //    if (updateDto.PostDocFile != null)
        //    {
        //        // احذف القديم لو موجود
        //        if (!string.IsNullOrEmpty(post.PostDocPath))
        //        {
        //            string oldPath = Path.Combine(Directory.GetCurrentDirectory(), post.PostDocPath.TrimStart('\\', '/'));
        //            if (File.Exists(oldPath))
        //                File.Delete(oldPath);
        //        }

        //        // احفظ الملف الجديد
        //        string newDocPath = await _mediaService.SaveFileAsync(updateDto.PostDocFile);
        //        post.PostDocPath = newDocPath;

        //        // ❗ مهم: Reset تقييم الذكاء الصناعي لأن المستند اتغير
        //        post.PostDocPathEvaluation = AIDecision.NotReviewed;
        //    }



        //    // 🔴 حذف الصور
        //    if (updateDto.ImagesToDelete != null && updateDto.ImagesToDelete.Any())
        //    {
        //        var imagesToDelete = post.PostImages
        //                                 .Where(pi => updateDto.ImagesToDelete.Contains(pi.ImageUrl))
        //                                 .ToList();

        //        foreach (var img in imagesToDelete)
        //        {
        //            // ❗ تحويل مسار الصورة لمسار فعلي كامل
        //            string fullPath = Path.Combine(Directory.GetCurrentDirectory(), img.ImageUrl.TrimStart('\\', '/'));

        //            if (File.Exists(fullPath))
        //                File.Delete(fullPath);

        //            post.PostImages.Remove(img);
        //        }
        //    }

        //    // 🟢 إضافة الصور الجديدة
        //    if (updateDto.NewImages != null)
        //    {
        //        foreach (var file in updateDto.NewImages.Where(f => f != null))
        //        {
        //            string imagePath = await _mediaService.SaveFileAsync(file);
        //            post.PostImages.Add(new PostImage { ImageUrl = imagePath });
        //        }
        //    }
        //    post.PendingStatus = PostPendingStatus.Pending;
        //    _postRepository.Update(post);
        //    await _postRepository.SaveChangesAsync();

        //    // 🛡 تأكد أن Landlord موجود ومعه بيانات اليوزر
        //    var landlord = await _userRepository.NestedFind(
        //                        l => l.UserId == post.LandlordId,
        //                        l => l.User
        //                    );
        //    var landlordEntity = landlord.FirstOrDefault();

        //    if (landlordEntity == null || landlordEntity.User == null)
        //        throw new Exception("Landlord or User data is missing");

        //    return MapToDTO(post, landlordEntity);
        //}
        #endregion

        private PostDTo MapToDTO(Post post, Landlord landlord)
        {
            string base64Doc = null;

             // //📄 قراءة ملف المستند وتحويله إلى Base64 إذا موجود
            //if (!string.IsNullOrEmpty(post.PostDocPath) && File.Exists(post.PostDocPath))
            //{
            //    byte[] fileBytes = File.ReadAllBytes(post.PostDocPath);
            //    base64Doc = Convert.ToBase64String(fileBytes);
            //}

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
                Email = landlord.User?.Email ?? "Unknown"
            };
        }


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

        public async Task<IEnumerable<ProposalDto>> GetLandlordProposalsAsync(long landlordId)
        {
            var landlordPosts = await _postRepository.FindAsync(p => p.LandlordId == landlordId);
            var postIds = landlordPosts.Select(p => p.PostId).ToList();

            var proposals = await _proposalRepository.NestedFind(
                p => postIds.Contains(p.PostId),
                p => p.User,
                p => p.Post,
                p => p.Post.PostImages);

            var result = new List<ProposalDto>();

            foreach (var proposal in proposals)
            {
                //string base64File = null;
                //if (!string.IsNullOrEmpty(proposal.FilePath) && File.Exists(proposal.FilePath))
                //{
                //    byte[] fileBytes = await File.ReadAllBytesAsync(proposal.FilePath);
                //    base64File = Convert.ToBase64String(fileBytes);
                //}

                result.Add(new ProposalDto
                {
                    ProposalId = proposal.ProposalId,
                    PostId = proposal.PostId,

                    // Post Data
                    Title = proposal.Post?.Title,
                    ImagePath = proposal.Post?.PostImages?.FirstOrDefault()?.ImageUrl,

                    TenantId = proposal.TenantId,
                    TenantName = proposal.User.UserName,
                    Phone = proposal.Phone,
                    StartRentalDate = proposal.StartRentalDate,
                    EndRentalDate = proposal.EndRentalDate,
                    ProposalStatus = proposal.ProposalStatus,
                    IsInstallment = proposal.IsInstallment,
                    FilePath = proposal.FilePath,
                    //FileName = Path.GetFileName(proposal.FilePath),
                    //FileBase64 = base64File
                });
            }

            return result;
        }

    }
}