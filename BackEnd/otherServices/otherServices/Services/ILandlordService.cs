using Microsoft.AspNetCore.Mvc;
using otherServices.Models;
using otherServices.Models.DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;
using WebAPIDotNet.DTOs;

namespace WebAPIDotNet.Services
{
    public interface ILandlordService
    {
        Task<PostDTo> Create_Post(int landlrdId,CreatePostDTO postDto);
        Task<PostDTo> Get_Post_By_Id(int id);
        Task<List<PostDTo>> Get_Posts_By_LandlordId(int landlordId);
        Task<bool> Delete_Post(long postId);
        Task<PostDTo> Update_Post(long postId, UpdatePostDTO updateDto);

        

        Task<Proposal> AcceptProposal(long proposalId);
        Task<Proposal> RejectProposal(long proposalId);

        Task<IEnumerable<ProposalDto>> GetLandlordProposalsAsync(long landlordId);
    }
}