using Microsoft.AspNetCore.Mvc;
using otherServices.Models;
using otherServices.Models.DTOs;
using otherServices.Models.DTOs.Posts;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebAPIDotNet.DTOs;

namespace otherServices.Services
{
    public interface ILandlordService
    {
        Task CreatePostAsync(long landlrdId, CreatePostDTO postDto);
        Task<PostDTo> Get_Post_By_Id(long id);
        Task<List<PostSummaryDto>> GetMyPostsAsync(long userId);
        Task Delete_Post(long postId);
        Task Update_Post(long postId, UpdatePostDTO updateDto);



        Task<AcceptProposalResponseDto> AcceptProposal(long proposalId);
        Task RejectProposal(long proposalId);

        Task<IEnumerable<ProposalDto>> GetLandlordProposalsAsync(long userId);
    }
}