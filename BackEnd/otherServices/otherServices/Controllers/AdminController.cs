// ===============================
// File: otherServices/Controllers/AdminController.cs
// ===============================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otherServices.Services;

namespace otherServices.Controllers
{
    [Authorize(Roles = "Admin")] // تأمين الكنترولر بالكامل للأدمن فقط
    [Route("api/admin/")]
    [ApiController]
    public class AdminController : BaseApiController // الوراثة من الكلاس الجديد
    {
        private readonly IAdminService _adminService;

        public AdminController(IAdminService adminService)
        {
            _adminService = adminService;
        }

        [HttpGet("all-user/")]
        public async Task<IActionResult> GetUsers()
        {
            try
            {
                var result = await _adminService.GetUsers();
                if (result == null || !result.Any())
                    return NotFound("Not found");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
      

        [HttpGet("verifiedWaitingLandlords")]
        public async Task<IActionResult> GetVerifiedWaitingLandlords()
        {
            try { return Ok(await _adminService.GetVerifiedWaitingLandlords()); }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpGet("rejectedLandlords")]
        public async Task<IActionResult> GetRejectedLandlords()
        {
            try { return Ok(await _adminService.GetRejectedLandlords()); }
            catch (Exception ex) { return BadRequest(new { error = ex.Message }); }
        }

        [HttpGet("waitingLandlords")]
        public async Task<IActionResult> GetWaitingLandlord()
        {
            try
            {
                var result = await _adminService.GetWaitingLandlord();
                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("landlord-status/{userId:long}")]
        public async Task<IActionResult> GetLanglordStatus(long userId)
        {
            try
            {
                var result = await _adminService.GetLandlordStatus(userId);
                if (result == null || !result.Any())
                    return NotFound();

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("accept-waiting-landlord/{userId:long}")]
        public async Task<IActionResult> AcceptUser(long userId)
        {
            try
            {
                await _adminService.AcceptUser(userId);
                return Ok(new { message = "User Accepted successfully" });
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { message = e.Message });
            }
            catch (InvalidOperationException e)
            {
                return Conflict(new { message = e.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error", details = ex.Message });
            }
        }

        [HttpPut("reject-waiting-landlord/{userId:long}")]
        public async Task<IActionResult> RejectUser(long userId)
        {
            try
            {
                await _adminService.RejectUser(userId);
                return Ok(new { message = "User Rejected successfully" });
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { message = e.Message });
            }
            catch (InvalidOperationException e)
            {
                return Conflict(new { message = e.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Unexpected error", details = ex.Message });
            }
        }

        [HttpGet("waitingPosts")]
        public async Task<IActionResult> GetWaitingPosts()
        {
            try
            {
                var result = await _adminService.GetWaitingPosts();
                if (result == null || !result.Any())
                    return NotFound("0 waiting Posts");

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("all-posts/")]
        public async Task<IActionResult> GetPost()
        {
            try
            {
                var result = await _adminService.GetPostsAsync();
                if (result == null || !result.Any())
                    return NotFound("Not found");

                return Ok(result);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("accept-post/{id:long}")]
        public async Task<IActionResult> AcceptPost(long id)
        {
            try
            {
                await _adminService.AcceptPost(id);
                return Ok(new { message = "Post Accepted successfully" });
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { message = e.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("reject-post/{id:long}")]
        public async Task<IActionResult> RejectPost(long id)
        {
            try
            {
                await _adminService.RejectPost(id);
                return Ok(new { message = "Post Rejected successfully" });
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { message = e.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("waitingCompanies")]
        public async Task<IActionResult> GetWaitingCompanies()
        {
            try
            {
                var result = await _adminService.GetWaitingCompanies();
                if (result == null || !result.Any())
                    return NotFound("0 waiting Companies");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("accept-company/{companyUserId:long}")]
        public async Task<IActionResult> AcceptCompany(long companyUserId)
        {
            try
            {
                return Ok(await _adminService.AcceptCompany(companyUserId));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("reject-company/{companyUserId:long}")]
        public async Task<IActionResult> RejectCompany(long companyUserId)
        {
            try
            {
                return Ok(await _adminService.RejectCompany(companyUserId));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("waitingProjects")]
        public async Task<IActionResult> GetWaitingProjects()
        {
            try
            {
                var result = await _adminService.GetWaitingProjects();
                if (result == null || !result.Any())
                    return NotFound("0 waiting Projects");

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("accept-project/{id:long}")]
        public async Task<IActionResult> AcceptProject(long id)
        {
            try
            {
                return Ok(await _adminService.AcceptProject(id));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("reject-project/{id:long}")]
        public async Task<IActionResult> RejectProject(long id)
        {
            try
            {
                return Ok(await _adminService.RejectProject(id));
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}