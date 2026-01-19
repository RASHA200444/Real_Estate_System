using Confluent.Kafka;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
//using otherServices.Data;
using otherServices.Models;
using otherServices.Models.DTOs;
using otherServices.Services;
using WebAPIDotNet.DTOs;
using WebAPIDotNet.Services;

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

        public LandlordController(AppDbContext2 db,ILandlordService landlordService)
        {
            this.landlordService = landlordService;
            this._db = db;
        }



        [HttpPost("create-post/{userId}")]
        public async Task<IActionResult> CreatePost(long userId, [FromForm] CreatePostDTO postDto)
        {
            try
            {
                await landlordService.CreatePostAsync(userId, postDto);
                return Ok(new { message = "Post created successfully, Wait for admin approval." });
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
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving the post" });
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
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while retrieving posts" });
            }
        }



        [HttpDelete("delete-post/{postId}")]
        public async Task<IActionResult> DeletePost(long postId)
        {
            try
            {
                var result = await landlordService.Delete_Post(postId);
                if (!result)
                    return NotFound(new { message = "Post not found" });

                return Ok(new { message = "Post deleted successfully" });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while deleting the post" });
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
            catch (Exception)
            {
                return StatusCode(500, new { message = "An error occurred while updating the post" });
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
                return StatusCode(500, new { message = "An error occurred while retrieving proposals", error = ex.Message });
            }
        }



        [HttpPut("accept-waiting-proposal/{proposalId}")]
        public async Task<IActionResult> AcceptProposal(long proposalId)
        {
            try
            {
                var acceptedProposal = await landlordService.AcceptProposal(proposalId);
                return Ok(acceptedProposal);
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { message = e.Message });
            }
        }



        [HttpPut("reject-waiting-proposal/{proposalId}")]
        public async Task<IActionResult> RejectProposal(long proposalId)
        {
            try
            {
                var rejectedProposal = await landlordService.RejectProposal(proposalId);
                return Ok(rejectedProposal);
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { message = e.Message });
            }
        }

    }
}
