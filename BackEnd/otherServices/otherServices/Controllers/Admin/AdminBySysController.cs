// ===============================
// File: otherServices/Controllers/Admin/AdminBySysController.cs
// ===============================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otherServices.Services.Interfaces;
using otherServices.Models.DTOs;

namespace otherServices.Controllers.Admin
{
    [Route("api/admin/sys")]
    [ApiController]
    [Authorize(Roles = "Admin")] // قفلنا المحبس: مبيفتحش غير للأدمن فقط
    public class AdminBySysController : BaseApiController // توحيد الوراثة
    {
        private readonly IAdminBySysService _adminService;

        public AdminBySysController(IAdminBySysService adminService)
        {
            _adminService = adminService;
        }

        // إنشاء أدمن جديد - عملية حساسة جداً
        [HttpPost("create")]
        public async Task<IActionResult> CreateAdmin([FromForm] CreateAdminDto dto)
        {
            try
            {
                await _adminService.CreateAdminAsync(dto);
                return Ok(new { message = "Admin created successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpGet("all")]
        public async Task<IActionResult> GetAllAdmins()
        {
            try
            {
                var admins = await _adminService.GetAllAsync();
                return Ok(admins);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{userId:long}")]
        public async Task<IActionResult> GetAdminById(long userId)
        {
            try
            {
                var admin = await _adminService.GetByUserIdAsync(userId);
                if (admin == null)
                    return NotFound(new { message = "Admin not found." });

                return Ok(admin);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("{userId:long}")]
        public async Task<IActionResult> UpdateAdmin(long userId, [FromForm] UpdateAdminDto dto)
        {
            try
            {
                var admin = await _adminService.UpdateAdminAsync(userId, dto);
                return Ok(new { message = "Admin Updated successfully.", data = admin });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpDelete("{userId:long}")]
        public async Task<IActionResult> DeleteAdmin(long userId)
        {
            try
            {
                await _adminService.DeleteAdminAsync(userId);
                return Ok(new { message = "Admin Deleted successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}