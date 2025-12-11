using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using otherServices.Models;
using otherServices.Models.DTOs;
using otherServices.Models.Enums;
using otherServices.Repositories;
using WebAPIDotNet.DTOs;

namespace otherServices.Services
{
    public class AdminService : IAdminService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILandlordRepository _landlordRepository;
        private readonly IPostRepository _postRepository;
        private readonly IWebHostEnvironment _env;
        private readonly AppDbContext2 _context;


        public AdminService(IUserRepository userRepository, ILandlordRepository landlordRepository, IPostRepository postRepository, IWebHostEnvironment env, AppDbContext2 context)
        {
            _landlordRepository = landlordRepository;
            _postRepository = postRepository;
            _env = env;
            _context = context;
            _userRepository = userRepository;
        }

        public async Task<Post> AcceptPost(long postId)
        {
            return await _postRepository.AcceptPostAsync(postId);
        }
        public async Task<Post> RejectPost(long postId)
        {
            return await _postRepository.RejectPostAsync(postId);
        }

        public async Task<Landlord> AcceptUser(long userId)
        {
            return await _landlordRepository.AcceptUserAsync(userId);
        }
        public async Task<Landlord> RejectUser(long userId)
        {
            return await _landlordRepository.RejectUserAsync(userId);
        }

        public async Task<IEnumerable<PostDTo>> GetWaitingPosts()
        {
            var posts = await _postRepository.NestedFind(
                p => p.PendingStatus == PostPendingStatus.Pending,  
                p => p.Landlord,
                p => p.PostImages,
                p => p.Landlord.User
            );

            posts = posts.Where(p => p.PostDocPathEvaluation == AIDecision.Uncertain);

            return posts.Select(p => new PostDTo
            {
                UserId = p.Landlord.UserId,
                UserName = p.Landlord.User.UserName,
                Email = p.Landlord.User.Email,

                PostId = p.PostId,
                Title = p.Title,
                Description = p.Description,
                Price = p.Price,
                PriceEvaluation= p.PriceEvaluation,
                Location = p.Location,
                LocationPath = p.LocationPath,
                RentalStatus = p.Status,
                DatePost = p.CreatedAt,
                FlagWaitingPost = p.PendingStatus,
                PostDocPathEvaluation = p.PostDocPathEvaluation,

                Images = p.PostImages.Select(pi => pi.ImageUrl).ToList(),
                PostDocPath = p.PostDocPath,
            });
        }



        public async Task<IEnumerable<WaitingLandlordsDto>> GetWaitingLandlord()
        {
            var users = await _context.Landlords
                                        .Include(l => l.User)
                                        .Where(l => l.PendingStatus == PendingStatus.Pending)
                                        .Where(l => l.OwnershipDocPathEvaluation == AIDecision.Uncertain)
                                        .ToListAsync();
            return users.Select(p => new WaitingLandlordsDto
            {
                UserId = p.UserId,
                LandlordId = p.LandlordId,
                UserName = p.User.UserName,
                Email = p.User.Email,
                OwnershipDocPath = p.OwnershipDocPath,
                OwnershipDocPathEvaluation = p.OwnershipDocPathEvaluation,
            });
        }

        public async Task<IEnumerable<Landlord>> GetLandlordStatus(long userid)
        {
            return await _landlordRepository.FindAsync(p => p.UserId == userid);
        }

        public async Task<IEnumerable<UserDto>> GetUsers()
        {
            var users = await _userRepository.GetAllAsync();

            return users.Select(u => new UserDto
            {
                UserId = u.UserId,
                UserName = u.UserName,
                Email = u.Email,
                Phone = u.Phone,
                Address = u.Address,
                RoleName = u.RoleName,
                NIDEvaluation = u.NIDEvaluation,
                CreatedAt = u.CreatedAt
            });
        }

    }
}