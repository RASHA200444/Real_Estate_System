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
        Task AcceptPost(long postId);
        Task RejectPost(long postId);
        
        Task AcceptUser(long UserId);
        Task RejectUser(long UserId);

        Task<IEnumerable<PostSummaryDto>> GetWaitingPosts();
        Task<IEnumerable<PostSummaryDto>> GetPostsAsync();

        Task<IEnumerable<WaitingLandlordsDto>> GetWaitingLandlord();
        Task<IEnumerable<Landlord>> GetLandlordStatus(long userid);

        Task<IEnumerable<UserDto>> GetUsers();


        Task<Company> AcceptCompany(long companyUserId);
        Task<Company> RejectCompany(long companyUserId);

        Task<IEnumerable<CompanyDto>> GetWaitingCompanies();

        Task<ProjectResponseDto> AcceptProject(long projectId);
        Task<ProjectResponseDto> RejectProject(long projectId);


        Task<IEnumerable<ProjectDto>> GetWaitingProjects();


    }
}