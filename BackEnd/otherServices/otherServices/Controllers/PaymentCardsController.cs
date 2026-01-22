using Microsoft.AspNetCore.Mvc;
using otherServices.Models.DTOs.Payments;
using otherServices.Services.Payments;

namespace otherServices.Controllers.Payments
{
    [Route("api/payments/cards")]
    [ApiController]
    public class PaymentCardsController : ControllerBase
    {
        private readonly IPaymentCardService _service;

        public PaymentCardsController(IPaymentCardService service)
        {
            _service = service;
        }

        // ✅ إضافة كارت (Tokenize) - multipart/form-data
        [HttpPost("tokenize/{userId}")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> Tokenize(long userId, [FromForm] TokenizeCardRequestDto dto)
        {
            try
            {
                var res = await _service.TokenizeAndSaveAsync(userId, dto);
                return Ok(res);
            }
            catch (Exception ex)
            {
                var msg = ex.InnerException?.Message ?? ex.Message;
                return BadRequest(new { error = msg });
            }

            
        }

        // ✅ كل الكروت
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetAll(long userId)
        {
            try
            {
                var res = await _service.GetAllAsync(userId);
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ✅ set default
        [HttpPut("{userId}/default/{paymentCardId}")]
        public async Task<IActionResult> SetDefault(long userId, long paymentCardId)
        {
            try
            {
                var res = await _service.SetDefaultAsync(userId, paymentCardId);
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // ✅ delete
        [HttpDelete("{userId}/{paymentCardId}")]
        public async Task<IActionResult> Delete(long userId, long paymentCardId)
        {
            try
            {
                var ok = await _service.DeleteAsync(userId, paymentCardId);
                return Ok(new { success = ok, message = "Card deleted permanently." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { success = false, error = ex.Message });
            }
        }
        // ✅ deactivate (soft delete)
        [HttpPut("{userId}/{paymentCardId}/deactivate")]
        public async Task<IActionResult> Deactivate(long userId, long paymentCardId)
        {
            try
            {
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
