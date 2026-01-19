using otherServices.Models;
using otherServices.Models.DTOs;
using otherServices.Models.DTOs.Posts;

namespace otherServices.Services
{
    public interface ITenantService
    {

        Task<ProposalDto> SubmitProposalAsync(long TenantId, long PostId, SubmitProposalDto form);
        Task<ProposalDto> EditProposalAsync(long proposalId, ProposalEditDto updated);
        Task<bool> DeleteProposalAsync(long proposalId);
        Task<bool> cancelSave(long userId,long postId);
        Task<IEnumerable<PostSummaryDto>> GetPostsAsync();
        Task<List<SavedPostDto>> GetMySavedPosts(long userId);
        Task<bool> Save_Post(long userId, long postId);

        Task<IEnumerable<ProposalDto>> GetTenantProposalsAsync(long userId);
        Task<LandlordDto> UpgradeToLandlord(long userId, LandlordUpgradeRequestDto dto);



    }
}
