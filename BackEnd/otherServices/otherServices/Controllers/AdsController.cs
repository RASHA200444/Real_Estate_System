using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otherServices.Models.DTOs.Ads;
using otherServices.Services.Ads;
using System.Security.Claims;

namespace otherServices.Controllers
{
    [ApiController]
    [Route("api/ads")]
    public class AdsController : ControllerBase
    {
        private readonly IAdvertisementService _service;

        public AdsController(IAdvertisementService service)
        {
            _service = service;
        }

        private long? GetUserId()
        {
            var idStr =
                User.FindFirstValue("uid") ??
                User.FindFirstValue(ClaimTypes.NameIdentifier) ??
                User.FindFirstValue("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier") ??
                User.FindFirstValue("nameid");

            if (!long.TryParse(idStr, out var userId) || userId <= 0)
                return null;

            return userId;
        }

        [Authorize]
        [HttpGet("popup")]
        public async Task<IActionResult> Popup()
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token (missing userId)" });

            var ad = await _service.GetPopupAdAsync(userId.Value);
            if (ad == null) return NoContent();

            return Ok(ad);
        }

        [Authorize]
        [HttpPost("click")]
        public async Task<IActionResult> Click([FromBody] ClickAdDto dto)
        {
            var userId = GetUserId();
            if (userId == null) return Unauthorized(new { message = "Invalid token (missing userId)" });

            await _service.TrackClickAsync(userId.Value, dto.AdId);
            return Ok(new { message = "Tracked" });
        }
    }
}
