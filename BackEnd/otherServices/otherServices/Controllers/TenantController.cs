// ===============================
// File: otherServices/Controllers/TenantController.cs
// ===============================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otherServices.Models.DTOs;
using otherServices.Services;

namespace otherServices.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // تأمين الكنترولر بالكامل
    public class TenantController : BaseApiController // الوراثة من الكلاس الموحد
    {
        private readonly ITenantService _tenantService;
        public TenantController(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }

        #region Proposal

        [HttpPost("submit-proposal/{postId:long}")]
        public async Task<IActionResult> SubmitProposal(long postId, [FromForm] SubmitProposalDto form)
        {
            try
            {
                if (RequireUserId(out var tenantId) is IActionResult error) return error;

                await _tenantService.SubmitProposalAsync(tenantId, postId, form);
                return Ok(new { message = "Proposal Sent successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.InnerException?.Message ?? ex.Message });
            }
        }

        [HttpGet("my-proposals")]
        public async Task<IActionResult> GetTenantProposals()
        {
            try
            {
                if (RequireUserId(out var tenantId) is IActionResult error) return error;

                var proposals = await _tenantService.GetTenantProposalsAsync(tenantId);
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

        [HttpPut("edit-proposal/{proposalId:long}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> EditProposal(long proposalId, [FromForm] ProposalEditDto updated)
        {
            try
            {
                // ملاحظة: الـ Service جوه المفروض تتأكد إن الـ proposalId ده ملك للـ User ده
                await _tenantService.EditProposalAsync(proposalId, updated);
                return Ok(new { message = "Proposal Updated successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("cancel-proposal/{proposalId:long}")]
        public async Task<IActionResult> DeleteProposal(long proposalId)
        {
            try
            {
                await _tenantService.DeleteProposalAsync(proposalId);
                return Ok(new { message = "Proposal deleted successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("payment-plan/{planId:long}")]
        public async Task<IActionResult> GetPlanDetails(long planId)
        {
            if (RequireUserId(out var tenantId) is IActionResult error) return error;

            var res = await _tenantService.GetPaymentPlanDetailsAsync(planId, tenantId);
            return Ok(res);
        }

        [HttpGet("payment-plans")]
        public async Task<IActionResult> GetPlans()
        {
            if (RequireUserId(out var tenantId) is IActionResult error) return error;

            var res = await _tenantService.GetMyPaymentPlansAsync(tenantId);
            return Ok(res);
        }

        #endregion

        #region Posts

        [HttpGet("all-posts")]
        [AllowAnonymous] // السماح للكل بمشاهدة العقارات
        public async Task<IActionResult> GetPosts()
        {
            try
            {
                var result = await _tenantService.GetPostsAsync();
                if (result == null || !result.Any())
                    return NotFound("No posts found");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost("save-post/{postId:long}")]
        public async Task<IActionResult> SavePost(long postId)
        {
            try
            {
                if (RequireUserId(out var userId) is IActionResult error) return error;

                await _tenantService.Save_Post(userId, postId);
                return Ok(new { message = "Post saved successfully" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpDelete("cancel-save/{postId:long}")]
        public async Task<IActionResult> CancelSave(long postId)
        {
            try
            {
                if (RequireUserId(out var userId) is IActionResult error) return error;

                await _tenantService.CancelSave(userId, postId);
                return Ok(new { message = "Post Unsaved Successfully" });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("my-saved-posts")]
        public async Task<IActionResult> GetMySavedPosts()
        {
            try
            {
                if (RequireUserId(out var userId) is IActionResult error) return error;

                var result = await _tenantService.GetMySavedPosts(userId);
                if (result == null || !result.Any())
                    return NotFound("No saved posts found");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        #endregion

        #region UpgradeToLandlord

        [HttpPost("upgrade-to-landlord")]
        public async Task<IActionResult> UpgradeToLandlord([FromForm] LandlordUpgradeRequestDto dto)
        {
            try
            {
                if (RequireUserId(out var userId) is IActionResult error) return error;

                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                await _tenantService.UpgradeToLandlord(userId, dto);
                return Ok(new { message = "Your Role Upgraded Successfully, Wait for Admin Approval." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        #endregion
    }
}