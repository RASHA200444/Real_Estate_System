using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using otherServices.Services.Interfaces;
using otherServices.Models.DTOs;
using otherServices.Models;


namespace otherServices.Controllers.Admin
{
    [Route("api/admin/sys")]
    [ApiController]
    public class AdminBySysController : ControllerBase
    {
        private readonly IAdminBySysService _adminService;

        public AdminBySysController(IAdminBySysService adminService)
        {
            _adminService = adminService;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateAdmin([FromForm] CreateAdminDto dto)
        {
            try { 
            //int createdBySystemAdminId = 1; 
            var newAdmin = await _adminService.CreateAdminAsync(dto);
            return Ok(new { success = true, message = "Admin created successfully.", data = newAdmin });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllAdmins()
        {
            try { 
            var admins = await _adminService.GetAllAsync();
            return Ok(new { success = true, data = admins });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{userId:long}")]
        public async Task<IActionResult> GetAdminById(long userId)
        {
            try { 
            var admin = await _adminService.GetByUserIdAsync(userId);
            if (admin == null)
                return NotFound(new { success = false, message = "Admin not found." });

            return Ok(new { success = true, data = admin });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{userId:long}")]
        public async Task<IActionResult> UpdateAdmin(long userId, [FromForm] UpdateAdminDto dto)
        {
            try { 
            var (success, message) = await _adminService.UpdateAdminAsync(userId, dto);
            return Ok(new { success, message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{userId:long}")]
        public async Task<IActionResult> DeleteAdmin(long userId)
        {
            try { 
            var (success, message) = await _adminService.DeleteAdminAsync(userId);
            return Ok(new { success, message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
