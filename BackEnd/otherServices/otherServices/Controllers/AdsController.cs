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

        [Authorize]
        [HttpGet("popup")]
        public async Task<IActionResult> Popup()
        {
            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (userId == 0) return Unauthorized();

            var ad = await _service.GetPopupAdAsync(userId);
            if (ad == null) return NoContent();

            return Ok(ad);
        }

        [Authorize]
        [HttpPost("click")]
        public async Task<IActionResult> Click([FromBody] ClickAdDto dto)
        {
            var userId = long.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "0");
            if (userId == 0) return Unauthorized();

            await _service.TrackClickAsync(userId, dto.AdId);
            return Ok(new { message = "Tracked" });
        }
    }
}
