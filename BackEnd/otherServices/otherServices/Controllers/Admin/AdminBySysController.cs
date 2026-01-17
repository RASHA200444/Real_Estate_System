using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using otherServices.Services.Interfaces;
using otherServices.Models.DTOs;


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
            int createdBySystemAdminId = 1; 
            var newAdmin = await _adminService.CreateAdminAsync(dto, createdBySystemAdminId);
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

        [HttpGet("{adminId:int}")]
        public async Task<IActionResult> GetAdminById(int adminId)
        {
            try { 
            var admin = await _adminService.GetByIdAsync(adminId);
            if (admin == null)
                return NotFound(new { success = false, message = "Admin not found." });

            return Ok(new { success = true, data = admin });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{adminId:int}")]
        public async Task<IActionResult> UpdateAdmin(int adminId, [FromForm] UpdateAdminDto dto)
        {
            try { 
            var (success, message) = await _adminService.UpdateAdminAsync(adminId, dto);
            return Ok(new { success, message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{adminId:int}")]
        public async Task<IActionResult> DeleteAdmin(int adminId)
        {
            try { 
            var (success, message) = await _adminService.DeleteAdminAsync(adminId);
            return Ok(new { success, message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
