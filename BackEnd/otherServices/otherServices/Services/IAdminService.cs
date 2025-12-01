using otherServices.Models;
using otherServices.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebAPIDotNet.DTOs;

namespace otherServices.Services
{
    public interface IAdminService
    {
        Task<Post> AcceptPost(long postId);
        Task<Post> RejectPost(long postId);
        
        Task<Landlord> AcceptUser(long UserId);
        Task<Landlord> RejectUser(long UserId);

        Task<IEnumerable<PostDTo>> GetWaitingPosts();

        Task<IEnumerable<WaitingLandlordsDto>> GetWaitingLandlord();
        Task<IEnumerable<Landlord>> GetLandlordStatus(long userid);

        Task<IEnumerable<UserDto>> GetUsers();




    }
}