// ===============================
// File: otherServices/Controllers/Admin/AdsAdminController.cs
// ===============================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otherServices.Models.DTOs.Ads;
using otherServices.Services.Ads;

namespace otherServices.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/ads")]
    [Authorize(Roles = "Admin")] // التأكد من أن المستخدم "أدمن"
    public class AdsAdminController : BaseApiController // الوراثة من الكلاس الموحد
    {
        private readonly IAdvertisementService _service;

        public AdsAdminController(IAdvertisementService service)
        {
            _service = service;
        }

        // ✅ List all ads (Admin panel)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            if (RequireUserId(out var adminId) is IActionResult error) return error;

            var res = await _service.GetAllAdsAsync(adminId);
            return Ok(res);
        }

        // ✅ Eligible posts that can be advertised
        [HttpGet("eligible-posts")]
        public async Task<IActionResult> EligiblePosts()
        {
            if (RequireUserId(out var adminId) is IActionResult error) return error;

            var res = await _service.GetEligiblePostsAsync(adminId);
            return Ok(res);
        }

        // ✅ Create New Ad
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAdDto dto)
        {
            if (RequireUserId(out var adminId) is IActionResult error) return error;

            var id = await _service.CreateAdAsync(adminId, dto);
            return Ok(new { adId = id });
        }

        // ✅ Toggle Ad Status
        [HttpPut("{adId:long}/toggle")]
        public async Task<IActionResult> Toggle(long adId, [FromQuery] bool isActive)
        {
            if (RequireUserId(out var adminId) is IActionResult error) return error;

            await _service.ToggleAdAsync(adminId, adId, isActive);
            return Ok(new { message = "Updated successfully" });
        }

        // ✅ Debug helper
        [HttpGet("whoami")]
        public IActionResult WhoAmI()
        {
            return Ok(User.Claims.Select(c => new { c.Type, c.Value }));
        }
    }
}