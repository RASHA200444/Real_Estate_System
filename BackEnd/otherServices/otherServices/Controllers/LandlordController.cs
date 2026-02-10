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
    // [Authorize] // اختياري: لو عندك JWT middleware شغال
    public class LandlordController : ControllerBase
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

        private long GetUserIdFromClaims()
        {
            var uid = User?.Claims?.FirstOrDefault(c => c.Type == "uid")?.Value;
            if (string.IsNullOrWhiteSpace(uid) || !long.TryParse(uid, out var userId))
                throw new UnauthorizedAccessException("Missing/invalid uid claim.");

            return userId;
        }

        [HttpPost("create-post")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> CreatePost([FromForm] CreatePostDTO postDto)
        {
            try
            {
                var landlordUserId = GetUserIdFromClaims();
                await _landlordService.CreatePostAsync(landlordUserId, postDto);
                return Ok(new { message = "Post created successfully, wait for admin approval." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (DbUpdateException ex)
            {
                var root = ex.GetBaseException().Message;
                return StatusCode(500, new { message = "Database update failed.", details = root });
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

        [HttpGet("get-post/{postId:long}")]
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
                var landlordUserId = GetUserIdFromClaims();
                var posts = await _landlordService.GetMyPostsAsync(landlordUserId);
                return Ok(posts);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
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

        [HttpDelete("delete-post/{postId:long}")]
        public async Task<IActionResult> DeletePost(long postId)
        {
            try
            {
                var landlordUserId = GetUserIdFromClaims();
                await _landlordService.Delete_Post(landlordUserId, postId);
                return Ok(new { message = "Post deleted successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { error = msg });
            }
        }

        [HttpPut("edit-post/{postId:long}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdatePost(long postId, [FromForm] UpdatePostDTO updateDto)
        {
            try
            {
                var landlordUserId = GetUserIdFromClaims();
                await _landlordService.Update_Post(landlordUserId, postId, updateDto);
                return Ok(new { message = "The post updated successfully, wait for admin approval." });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
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

        [HttpGet("proposals")]
        public async Task<IActionResult> GetProposalsForLandlord()
        {
            try
            {
                var landlordUserId = GetUserIdFromClaims();
                var proposals = await _landlordService.GetLandlordProposalsAsync(landlordUserId);
                return Ok(proposals);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
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

        [HttpPut("accept-waiting-proposal/{proposalId:long}")]
        public async Task<IActionResult> AcceptProposal(long proposalId)
        {
            try
            {
                var landlordUserId = GetUserIdFromClaims();
                var res = await _landlordService.AcceptProposal(landlordUserId, proposalId);
                return Ok(new { success = true, message = "Proposal accepted successfully", data = res });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { success = false, message = ex.Message });
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

        [HttpPut("reject-waiting-proposal/{proposalId:long}")]
        public async Task<IActionResult> RejectProposal(long proposalId)
        {
            try
            {
                var landlordUserId = GetUserIdFromClaims();
                await _landlordService.RejectProposal(landlordUserId, proposalId);
                return Ok(new { message = "Proposal rejected successfully" });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { message = ex.Message });
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { message = e.Message });
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
                var landlordUserId = GetUserIdFromClaims();
                var res = await _paymentFlow.SubscribeProAsync(landlordUserId, dto);
                return Ok(res);
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { error = msg });
            }
        }
    }
}
