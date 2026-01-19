using Microsoft.AspNetCore.Mvc;
using otherServices.Models;
using otherServices.Models.DTOs;
using otherServices.Models.DTOs.Posts;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebAPIDotNet.DTOs;

namespace WebAPIDotNet.Services
{
    public interface ILandlordService
    {
        Task<string> Create_Post(long landlrdId,CreatePostDTO postDto);
        Task<PostDTo> Get_Post_By_Id(long id);
        Task<List<PostSummaryDto>> GetMyPostsAsync(long userId);
        Task<bool> Delete_Post(long postId);
        Task<PostDTo> Update_Post(long postId, UpdatePostDTO updateDto);

        

        Task<Proposal> AcceptProposal(long proposalId);
        Task<Proposal> RejectProposal(long proposalId);

        Task<IEnumerable<ProposalDto>> GetLandlordProposalsAsync(long userId);
    }
}