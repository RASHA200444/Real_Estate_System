using otherServices.Models.DTOs;
using otherServices.Models.DTOs.Posts;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebAPIDotNet.DTOs;

namespace otherServices.Services
{
    public interface ILandlordService
    {
        // Posts
        Task CreatePostAsync(long landlordUserId, CreatePostDTO postDto);
        Task<PostDTo> Get_Post_By_Id(long postId);
        Task<List<PostSummaryDto>> GetMyPostsAsync(long landlordUserId);

        Task Delete_Post(long landlordUserId, long postId);
        Task Update_Post(long landlordUserId, long postId, UpdatePostDTO updateDto);

        // Proposals
        Task<IEnumerable<ProposalDto>> GetLandlordProposalsAsync(long landlordUserId);

        Task<AcceptProposalResponseDto> AcceptProposal(long landlordUserId, long proposalId);
        Task RejectProposal(long landlordUserId, long proposalId);
    }
}
