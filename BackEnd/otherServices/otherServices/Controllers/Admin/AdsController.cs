using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otherServices.Models.DTOs.Ads;
using otherServices.Services.Ads;
using System.Security.Claims;

namespace otherServices.Controllers.Admin
{
    [ApiController]
    [Route("api/admin/ads")]
    [Authorize(Roles = "Admin")]
    public class AdsAdminController : ControllerBase
    {
        private readonly IAdvertisementService _service;

        public AdsAdminController(IAdvertisementService service)
        {
            _service = service;
        }

        private long GetUserId()
        {
            var idStr =
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("uid") ??
                User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier") ??
                User.FindFirstValue("nameid");

            if (!long.TryParse(idStr, out var userId))
                throw new Exception("Invalid token: user id claim is missing/invalid");

            return userId;
        }

        // ✅ NEW: List all ads (Admin panel)
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var adminUserId = GetUserId();
            var res = await _service.GetAllAdsAsync(adminUserId);
            return Ok(res);
        }

        // ✅ NEW: Eligible posts that can be advertised
        [HttpGet("eligible-posts")]
        public async Task<IActionResult> EligiblePosts()
        {
            var adminUserId = GetUserId();
            var res = await _service.GetEligiblePostsAsync(adminUserId);
            return Ok(res);
        }

        // existing
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateAdDto dto)
        {
            var adminUserId = GetUserId();
            var id = await _service.CreateAdAsync(adminUserId, dto);
            return Ok(new { adId = id });
        }

        // existing
        [HttpPut("{adId}/toggle")]
        public async Task<IActionResult> Toggle(long adId, [FromQuery] bool isActive)
        {
            var adminUserId = GetUserId();
            await _service.ToggleAdAsync(adminUserId, adId, isActive);
            return Ok(new { message = "Updated" });
        }

        // debug
        [Authorize]
        [HttpGet("whoami")]
        public IActionResult WhoAmI()
        {
            return Ok(User.Claims.Select(c => new { c.Type, c.Value }));
        }
    }
}
