// File: otherServices/Services/IAdminService.cs
using otherServices.Models;
using otherServices.Models.DTOs;
using otherServices.Models.DTOs.Posts;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebAPIDotNet.DTOs;

namespace otherServices.Services
{
    public interface IAdminService
    {
        // ========================= POSTS =========================
        Task AcceptPost(long postId);
        Task RejectPost(long postId);
        Task<IEnumerable<WaitingPostsDto>> GetWaitingPosts();
        Task<IEnumerable<AllPostsDto>> GetPostsAsync();

        // ========================= LANDLORDS =========================
        Task AcceptUser(long UserId);
        Task RejectUser(long UserId);
        Task<IEnumerable<WaitingLandlordsDto>> GetWaitingLandlord(); // AI Uncertain
        Task<IEnumerable<WaitingLandlordsDto>> GetVerifiedWaitingLandlords(); // AI Verified (الجديد)
        Task<IEnumerable<WaitingLandlordsDto>> GetRejectedLandlords(); // Rejected/Fraud (الجديد)
        Task<IEnumerable<Landlord>> GetLandlordStatus(long userid);

        // ========================= USERS =========================
        Task<IEnumerable<UserDto>> GetUsers();

        // ========================= COMPANIES =========================
        Task<Company> AcceptCompany(long companyUserId);
        Task<Company> RejectCompany(long companyUserId);
        Task<IEnumerable<CompanyDto>> GetWaitingCompanies();

        // ========================= PROJECTS =========================
        Task<ProjectResponseDto> AcceptProject(long projectId);
        Task<ProjectResponseDto> RejectProject(long projectId);
        Task<IEnumerable<ProjectDto>> GetWaitingProjects();
    }
}