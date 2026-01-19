using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

using otherServices.Models;
using otherServices.Models.DTOs;
using otherServices.Models.Enums;
using otherServices.Services;
using WebAPIDotNet.DTOs;
using WebAPIDotNet.Services;


namespace otherServices.Controllers
{
    //[Authorize(Roles = "tenant")]
    [Route("api/[controller]")]
    [ApiController]
    //[Authorize]
    public class TenantController:ControllerBase
    {
        private readonly ITenantService _tenantService;
        public TenantController(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }


        [HttpPost("submit-proposal/{PostId}/{TenantId}")]
        public async Task<IActionResult> SubmitProposal(long TenantId, long PostId, [FromForm] SubmitProposalDto form)
        {
            try
            {
                var result = await _tenantService.SubmitProposalAsync(TenantId,PostId,form);
                return Ok(new { message = result });
            }
            catch (Exception ex)
            {
                var errorMessage = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { error = errorMessage });
            }
        }

        [HttpGet("my-proposals/{tenantId}")]
        public async Task<IActionResult> GetProposalsForLandlord(long tenantId)
        {
            try
            {
                var proposals = await _tenantService.GetTenantProposalsAsync(tenantId);
                if (proposals == null)
                {
                    return NotFound(new { message = "No proposals found for this tenant" });
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


        [HttpPut("edit-proposal/{proposalId}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> EditProposal(long proposalId, [FromForm] ProposalEditDto updated)
        {
            try
            {
                var proposalDto = await _tenantService.EditProposalAsync(proposalId, updated);
                if (proposalDto == null)
                    return NotFound("Proposal not found");

                return Ok(proposalDto);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(ex.Message);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }



        [HttpDelete("cancel-proposal/{proposalId}")]
        public async Task<IActionResult> DeleteProposal(long proposalId)
        {
            try { 
            var success = await _tenantService.DeleteProposalAsync(proposalId);
            if (!success) return NotFound("Proposal not found");
            return Ok("Proposal deleted");
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }



        [HttpGet("all-posts/")]
        public async Task<IActionResult> GetPost()
        {
            var result = await _tenantService.GetPostsAsync();
            if (result == null || !result.Any())
            {
                return NotFound("Not found");
            }
            else
            {
                return Ok(result);
            }
        }



        [HttpPost("{UserId}/save-post/{postId}")]
        public async Task<IActionResult> SavePost(
    [FromRoute] long UserId,
    [FromRoute] long postId)
        {
            try
            {
                await _tenantService.Save_Post(UserId, postId);

                return Ok(new
                {
                    message = "Post saved successfully"
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = ex.Message
                });
            }
        }

        [HttpDelete("{userId}/cancel-save/{postId}")]
        public async Task<IActionResult> cancelSave(long userId,long postId)
        {
            try { 
                await _tenantService.cancelSave(userId,postId);
                return Ok(new
                {
                    message = "Post Unsaved Successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }


        [HttpGet("My-saved-posts/{UserId}")]
        public async Task<IActionResult> GetMySavedPosts(long UserId)
        {
            try { 
            var result = await _tenantService.GetMySavedPosts(UserId);
            if (result == null || !result.Any())
            {
                return NotFound("Not found");
            }
            else
            {
                return Ok(result);
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("upgrade-to-landlord/{userId}")]
        public async Task<IActionResult> UpgradeToLandlord(long userId, [FromForm] LandlordUpgradeRequestDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                await _tenantService.UpgradeToLandlord(userId, dto);
                return Ok(new
                {
                    message = "Your Role Upgraded Successfully, Wait for Admin Approval."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }


    }
}
