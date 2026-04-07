// ===============================
// File: otherServices/Controllers/Payments/PaymentCardsController.cs
// ===============================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otherServices.Models.DTOs.Payments;
using otherServices.Services.Payments;

namespace otherServices.Controllers.Payments
{
    [Route("api/payments/cards")]
    [ApiController]
    [Authorize] // حماية إجبارية لكل عمليات البطاقات
    public class PaymentCardsController : BaseApiController // الوراثة من الكلاس الجديد
    {
        private readonly IPaymentCardService _service;

        public PaymentCardsController(IPaymentCardService service)
        {
            _service = service;
        }

        // ✅ إضافة كارت (Tokenize)
        // تم حذف {userId} من المسار
        [HttpPost("tokenize")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Tokenize([FromForm] TokenizeCardRequestDto dto)
        {
            try
            {
                if (RequireUserId(out var userId) is IActionResult error) return error;

                var res = await _service.TokenizeAndSaveAsync(userId, dto);
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.InnerException?.Message ?? ex.Message });
            }
        }

        // ✅ عرض كل الكروت الخاصة بالمستخدم الحالي
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            try
            {
                if (RequireUserId(out var userId) is IActionResult error) return error;

                var res = await _service.GetAllAsync(userId);
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ✅ تعيين كارت كافتراضي
        [HttpPut("default/{paymentCardId}")]
        public async Task<IActionResult> SetDefault(long paymentCardId)
        {
            try
            {
                if (RequireUserId(out var userId) is IActionResult error) return error;

                var res = await _service.SetDefaultAsync(userId, paymentCardId);
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ✅ حذف نهائي
        [HttpDelete("{paymentCardId}")]
        public async Task<IActionResult> Delete(long paymentCardId)
        {
            try
            {
                if (RequireUserId(out var userId) is IActionResult error) return error;

                var ok = await _service.DeleteAsync(userId, paymentCardId);
                return Ok(new { success = ok, message = "Card deleted permanently." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }

        // ✅ إيقاف تنشيط (Soft Delete)
        [HttpPut("{paymentCardId}/deactivate")]
        public async Task<IActionResult> Deactivate(long paymentCardId)
        {
            try
            {
                if (RequireUserId(out var userId) is IActionResult error) return error;

                var ok = await _service.DeactivateAsync(userId, paymentCardId);
                return Ok(new { success = ok, message = "Card deactivated." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }
    }
}