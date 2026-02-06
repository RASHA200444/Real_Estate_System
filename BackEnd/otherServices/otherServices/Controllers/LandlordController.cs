using Confluent.Kafka;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
//using otherServices.Data;
using otherServices.Models;
using otherServices.Models.DTOs;
using otherServices.Models.DTOs.Payments;
using otherServices.Services;
using otherServices.Services.Payments;
using WebAPIDotNet.DTOs;

//using otherServices.Data.Models;

namespace otherServices.Controllers
{
    //[Authorize(Roles = "landlord")]
    [Route("api/[controller]")]
    [ApiController]
    public class LandlordController : ControllerBase
    {
        private readonly AppDbContext2 _db;
        private readonly ILandlordService landlordService;
        private readonly IPaymentFlowService _paymentFlow;


        public LandlordController(
            AppDbContext2 db,
            ILandlordService landlordService,
            IPaymentFlowService paymentFlow) // ✅ NEW
        {
            this.landlordService = landlordService;
            this._db = db;
            _paymentFlow = paymentFlow;
        }



        [HttpPost("create-post/{userId}")]
        public async Task<IActionResult> CreatePost(long userId, [FromForm] CreatePostDTO postDto)
        {
            try
            {
                await landlordService.CreatePostAsync(userId, postDto);
                return Ok(new { message = "Post created successfully, Wait for admin approval." });
            }
            catch (DbUpdateException ex)
            {
                var root = ex.GetBaseException().Message; // أهم سطر
                return StatusCode(500, new
                {
                    message = "Database update failed.",
                    details = root
                });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }




        [HttpGet("get-post/{postId}")]
        public async Task<IActionResult> GetPostById(long postId)
        {
            try
            {
                var post = await landlordService.Get_Post_By_Id(postId);
                return Ok(post);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }



        [HttpGet("get-my-posts/{userId}")]
        public async Task<IActionResult> GetPostsByUser(long userId)
        {
            try
            {
                var posts = await landlordService.GetMyPostsAsync(userId);
                return Ok(posts);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }



        [HttpDelete("delete-post/{postId}")]
        public async Task<IActionResult> DeletePost(long postId)
        {
            try
            {
                await landlordService.Delete_Post(postId);
                return Ok(new { message = "Post deleted successfully" });
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { error = errorMessage });
            }
        }



        [HttpPut("edit-post/{postId}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdatePost(long postId, [FromForm] UpdatePostDTO updateDto)
        {
            try
            {
                await landlordService.Update_Post(postId, updateDto);
                return Ok(new { message = "The post Updated successfully, Wait for admin approval." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }


        
        [HttpGet("proposals/{userId}")]
        public async Task<IActionResult> GetProposalsForLandlord(long userId)
        {
            try
            {
                var proposals = await landlordService.GetLandlordProposalsAsync(userId);
                if (proposals == null)
                {
                    return NotFound(new { message = "No proposals found for this landlord" });
                }
                return Ok(proposals);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }



        [HttpPut("accept-waiting-proposal/{proposalId}")]
        public async Task<IActionResult> AcceptProposal(long proposalId)
        {
            try
            {
                var res = await landlordService.AcceptProposal(proposalId);
                return Ok(new { success = true, message = "Proposal Accepted successfully", data = res });
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { success = false, message = e.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }




        [HttpPut("reject-waiting-proposal/{proposalId}")]
        public async Task<IActionResult> RejectProposal(long proposalId)
        {
            try
            {
                await landlordService.RejectProposal(proposalId);
                return Ok(new { message = "Proposal Rejected successfully" });
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { message = e.Message });
            }
        }

        // ✅ NEW: Subscribe Pro (Landlord chooses plan + card)
        [HttpPost("subscribe-pro/{landlordUserId}")]
        public async Task<IActionResult> SubscribePro(long landlordUserId, [FromBody] SubscribeProRequestDto dto)
        {
            try
            {
                var res = await _paymentFlow.SubscribeProAsync(landlordUserId, dto);
                return Ok(res);
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { error = msg });
            }
        }
    }
}
