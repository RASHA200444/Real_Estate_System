// ===============================
// File: otherServices/Controllers/ProfileController.cs
// ===============================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otherServices.Services;
using otherServices.Models.DTOs;
using Microsoft.EntityFrameworkCore;

namespace otherServices.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // حماية عامة للبروفايل
    public class ProfileController : BaseApiController // الوراثة من الكلاس الموحد
    {
        private readonly IProfileService _service;

        public ProfileController(IProfileService service)
        {
            _service = service;
        }

        // ✅ GET: api/profile/me
        // شيلنا الـ userId من الـ Route تماماً
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            try
            {
                if (RequireUserId(out var userId) is IActionResult error) return error;

                var result = await _service.GetMyProfileAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ✅ GET: api/profile/{userId}
        // دي بنسيبها لو مسموح لحد يشوف بروفايل حد تاني (Public Profile)
        [HttpGet("{userId:long}")]
        public async Task<IActionResult> GetUserProfile(long userId)
        {
            try
            {
                var result = await _service.GetUserProfileAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ✅ PUT: api/profile/me
        [HttpPut("me")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdateMyProfile([FromForm] UpdateProfileDto dto)
        {
            try
            {
                if (RequireUserId(out var userId) is IActionResult error) return error;

                await _service.UpdateMyProfileAsync(userId, dto);
                return Ok(new { message = "Profile updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ✅ PUT: api/profile/me/password
        [HttpPut("me/password")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UpdatePassword([FromForm] UpdatePasswordDto dto)
        {
            try
            {
                if (RequireUserId(out var userId) is IActionResult error) return error;

                await _service.UpdatePasswordAsync(userId, dto);
                return Ok(new { success = true, message = "Password updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ✅ DELETE: api/profile/me
        [HttpDelete("me")]
        public async Task<IActionResult> DeleteProfile([FromBody] string password)
        {
            try
            {
                if (RequireUserId(out var userId) is IActionResult error) return error;

                await _service.DeleteProfileAsync(userId, password);
                return Ok(new
                {
                    success = true,
                    message = "Account deleted successfully."
                });
            }
            catch (DbUpdateException ex)
            {
                return StatusCode(500, new { error = ex.InnerException?.Message ?? ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return Unauthorized(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}