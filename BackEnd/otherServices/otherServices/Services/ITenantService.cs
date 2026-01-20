using otherServices.Models;
using otherServices.Models.DTOs;
using otherServices.Models.DTOs.Posts;

namespace otherServices.Services
{
    public interface ITenantService
    {
        Task SubmitProposalAsync(long TenantId, long PostId, SubmitProposalDto form);
        Task EditProposalAsync(long proposalId, ProposalEditDto updated);
        Task DeleteProposalAsync(long proposalId);
        Task<IEnumerable<ProposalDto>> GetTenantProposalsAsync(long userId);


        Task<IEnumerable<PostSummaryDto>> GetPostsAsync();

        Task<List<PostSummaryDto>> GetMySavedPosts(long userId);
        Task Save_Post(long userId, long postId);
        Task CancelSave(long userId, long postId);

        Task UpgradeToLandlord(long userId, LandlordUpgradeRequestDto dto);



    }
}
