// ===============================
// File: otherServices/Controllers/LandlordController.cs
// ===============================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using otherServices.Models.DTOs;
using otherServices.Models.DTOs.Payments;
using otherServices.Services;
using otherServices.Services.Payments;

namespace otherServices.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // تفعيل الحماية بشكل عام على الكنترولر
    public class LandlordController : BaseApiController
    {
        private readonly ILandlordService _landlordService;
        private readonly IPaymentFlowService _paymentFlow;

        public LandlordController(
            ILandlordService landlordService,
            IPaymentFlowService paymentFlow)
        {
            _landlordService = landlordService;
            _paymentFlow = paymentFlow;
        }

        [HttpPost("create-post")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreatePost([FromForm] CreatePostDTO postDto)
        {
            try
            {
                if (RequireUserId(out var landlordUserId) is IActionResult error) return error;

                await _landlordService.CreatePostAsync(landlordUserId, postDto);
                return Ok(new { message = "Post created successfully, wait for admin approval." });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { message = "Database update failed.", details = ex.GetBaseException().Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("get-post/{postId:long}")]
        [AllowAnonymous] // لو حابب إن أي حد يشوف البوست حتى لو مش عامل login
        public async Task<IActionResult> GetPostById(long postId)
        {
            try
            {
                var post = await _landlordService.Get_Post_By_Id(postId);
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

        [HttpGet("get-my-posts")]
        public async Task<IActionResult> GetMyPosts()
        {
            try
            {
                if (RequireUserId(out var landlordUserId) is IActionResult error) return error;

                var posts = await _landlordService.GetMyPostsAsync(landlordUserId);
                return Ok(posts);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpDelete("delete-post/{postId:long}")]
        public async Task<IActionResult> DeletePost(long postId)
        {
            try
            {
                if (RequireUserId(out var landlordUserId) is IActionResult error) return error;

                await _landlordService.Delete_Post(landlordUserId, postId);
                return Ok(new { message = "Post deleted successfully" });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpPut("edit-post/{postId:long}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdatePost(long postId, [FromForm] UpdatePostDTO updateDto)
        {
            try
            {
                if (RequireUserId(out var landlordUserId) is IActionResult error) return error;

                await _landlordService.Update_Post(landlordUserId, postId, updateDto);
                return Ok(new { message = "The post updated successfully, wait for admin approval." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet("proposals")]
        public async Task<IActionResult> GetProposalsForLandlord()
        {
            try
            {
                if (RequireUserId(out var landlordUserId) is IActionResult error) return error;

                var proposals = await _landlordService.GetLandlordProposalsAsync(landlordUserId);
                return Ok(proposals);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpPut("accept-waiting-proposal/{proposalId:long}")]
        public async Task<IActionResult> AcceptProposal(long proposalId)
        {
            try
            {
                if (RequireUserId(out var landlordUserId) is IActionResult error) return error;

                var res = await _landlordService.AcceptProposal(landlordUserId, proposalId);
                return Ok(new { success = true, message = "Proposal accepted successfully", data = res });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        [HttpPut("reject-waiting-proposal/{proposalId:long}")]
        public async Task<IActionResult> RejectProposal(long proposalId)
        {
            try
            {
                if (RequireUserId(out var landlordUserId) is IActionResult error) return error;

                await _landlordService.RejectProposal(landlordUserId, proposalId);
                return Ok(new { message = "Proposal rejected successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPost("subscribe-pro")]
        public async Task<IActionResult> SubscribePro([FromBody] SubscribeProRequestDto dto)
        {
            try
            {
                if (RequireUserId(out var landlordUserId) is IActionResult error) return error;

                var res = await _paymentFlow.SubscribeProAsync(landlordUserId, dto);
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.InnerException?.Message ?? ex.Message });
            }
        }
    }
}