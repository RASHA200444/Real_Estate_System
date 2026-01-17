using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using otherServices.Services;
using otherServices.Models.DTOs;
using Microsoft.EntityFrameworkCore;
namespace otherServices.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProfileController : ControllerBase
    {
        private readonly IProfileService _service;

        public ProfileController(IProfileService service)
        {
            _service = service;
        }

        [HttpGet("me/{userId}")]
        public async Task<IActionResult> GetMyProfile(int userId)
        {
            try
            {
                var result = await _service.GetMyProfileAsync(userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{userId}")]
        public async Task<IActionResult> GetUserProfile(int userId)
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

        [HttpPut("me/{userId}")]
        public async Task<IActionResult> UpdateMyProfile(int userId, [FromForm] UpdateProfileDto dto)
        {
            try
            {
                await _service.UpdateMyProfileAsync(userId, dto);
                return Ok(new { message = "Profile updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("me/{userId}/password")]
        public async Task<IActionResult> UpdatePassword( int userId, [FromForm] UpdatePasswordDto dto)
        {
            try
            {
                await _service.UpdatePasswordAsync(userId, dto);
                return Ok(new { success = true, message = "Password updated successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }


        [HttpDelete("me/{userId}")]
        public async Task<IActionResult> DeleteProfile(int userId, [FromBody] string password)
        {
            try
            {
                await _service.DeleteProfileAsync(userId, password);
                return Ok(new
                {
                    success = true,
                    message = "Account deleted successfully."
                });
            }
            catch (DbUpdateException ex)
            {
                throw new Exception(ex.InnerException?.Message ?? ex.Message);
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
