// ===============================
// File: otherServices/Controllers/AdsController.cs
// ===============================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otherServices.Models.DTOs.Ads;
using otherServices.Services.Ads;
using System.Security.Claims;

namespace otherServices.Controllers
{
    [ApiController]
    [Route("api/ads")]
    public class AdsController : BaseApiController // الوراثة من الكلاس الجديد
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
            // التحقق من وجود المستخدم واستخراج الـ ID في خطوة واحدة
            if (RequireUserId(out var userId) is IActionResult error)
                return error;

            var ad = await _service.GetPopupAdAsync(userId);
            if (ad == null) return NoContent();

            return Ok(ad);
        }

        [Authorize]
        [HttpPost("click")]
        public async Task<IActionResult> Click([FromBody] ClickAdDto dto)
        {
            // التحقق من وجود المستخدم واستخراج الـ ID في خطوة واحدة
            if (RequireUserId(out var userId) is IActionResult error)
                return error;

            await _service.TrackClickAsync(userId, dto.AdId);
            return Ok(new { message = "Tracked" });
        }
    }
}