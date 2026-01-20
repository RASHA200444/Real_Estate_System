
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using otherServices.Models;
using otherServices.Services;
namespace otherServices.Controllers
{
    //[Authorize(Roles = "Admin")]
    [Route("api/admin/")]
    [ApiController]
    public class AdminController :ControllerBase
    {

        private readonly IAdminService _adminService;
        public AdminController(IAdminService adminService)
        {
            this._adminService = adminService;
        }


        [HttpGet("all-user/")]
        public async Task<IActionResult> GetUsers()
        {
            try { 
            var result = await _adminService.GetUsers();  
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

        [HttpGet("waitingLandlords")]
        public async Task<IActionResult> GetWaitingLandlord()
        {
            try { 
            var result = await _adminService.GetWaitingLandlord();
            return Ok(result);                
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("landlord-status/{userId}")]
        public async Task<IActionResult> GetLanglordStatus(long userId)
        {
            try { 
                var result = await _adminService.GetLandlordStatus(userId);
                if (result == null || !result.Any())
                {
                    return NotFound();
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

        [HttpPut("accept-waiting-landlord/{landlordId}")]
        public async Task<IActionResult> AcceptUser(long landlordId)
        {
            try
            {
                await _adminService.AcceptUser(landlordId);
                return Ok(new { message = "User Accepted successfully" });
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { message = e.Message });
            }
        }

        [HttpPut("reject-waiting-landlord/{landlordId}")]
        public async Task<IActionResult> RejectUser(long landlordId)
        {
            try
            {
                await _adminService.RejectUser(landlordId);
                return Ok(new { message = "User Rejected successfully" });
            }
            catch (KeyNotFoundException e)
            {
                return NotFound(new { message = e.Message });
            }
        }


        [HttpGet("waitingPosts")]
        public async Task<IActionResult> GetWaitingPosts()
        {
            try { 
                var result = await _adminService.GetWaitingPosts();
                if (result == null || !result.Any())
                {
                    return NotFound("0 waiting Posts");
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

        [HttpGet("all-posts/")]
        public async Task<IActionResult> GetPost()
        {
            var result = await _adminService.GetPostsAsync();
            if (result == null || !result.Any())
            {
                return NotFound("Not found");
            }
            else
            {
                return Ok(result);
            }
        }

        [HttpPut("accept-post/{id}")]
        public async Task<IActionResult> AcceptPost(int id)
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
        }

        [HttpPut("reject-post/{id}")]
        public async Task<IActionResult> RejectPost(int id)
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
        }
        [HttpGet("waitingCompanies")]
        public async Task<IActionResult> GetWaitingCompanies()
        {
            var result = await _adminService.GetWaitingCompanies();
            if (result == null || !result.Any()) return NotFound("0 waiting Companies");
            return Ok(result);
        }

        [HttpPut("accept-company/{companyUserId}")]
        public async Task<IActionResult> AcceptCompany(long companyUserId)
        {
            return Ok(await _adminService.AcceptCompany(companyUserId));
        }

        [HttpPut("reject-company/{companyUserId}")]
        public async Task<IActionResult> RejectCompany(long companyUserId)
        {
            return Ok(await _adminService.RejectCompany(companyUserId));
        }

        [HttpGet("waitingProjects")]
        public async Task<IActionResult> GetWaitingProjects()
        {
            var result = await _adminService.GetWaitingProjects();
            if (result == null || !result.Any()) return NotFound("0 waiting Projects");
            return Ok(result);
        }

        [HttpPut("accept-project/{id}")]
        public async Task<IActionResult> AcceptProject(long id)
        {
            return Ok(await _adminService.AcceptProject(id));
        }

        [HttpPut("reject-project/{id}")]
        public async Task<IActionResult> RejectProject(long id)
        {
            return Ok(await _adminService.RejectProject(id));
        }





    }
}
