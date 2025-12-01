using otherServices.Models;
using otherServices.Models.DTOs;

namespace otherServices.Services
{
    public interface ITenantService
    {

        Task<ProposalDto> SubmitProposalAsync(long TenantId, long PostId, SubmitProposalDto form);
        Task<ProposalDto> EditProposalAsync(long proposalId, ProposalEditDto updated);
        Task<bool> DeleteProposalAsync(long proposalId);
        Task<bool> cancelSave(long userId,long postId);
        Task<IEnumerable<PostDTo>> GetPosts();
        Task<List<SavedPostDto>> GetMySavedPosts(long userId);
        Task<bool> Save_Post(long userId, long postId);

        Task<IEnumerable<ProposalDto>> GetTenantProposalsAsync(long userId);


    }
}
