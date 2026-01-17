using Microsoft.AspNetCore.Mvc;
using otherServices.Services.Tenants;
using otherServices.Models.DTOs.CreditCards;
using otherServices.Services.Interfaces.Tenants;

namespace otherServices.Controllers.Tenant
{
    [Route("api/[controller]")]
    [ApiController]
    public class CreditCardController : ControllerBase
    {
        private readonly ICreditCardService _creditService;

        public CreditCardController(ICreditCardService creditService)
        {
            _creditService = creditService;
        }

        // POST: api/CreditCards/add/5
        [HttpPost("add/{tenantId}")]
        public async Task<IActionResult> AddCreditCard(int tenantId, [FromForm] CreateCreditCardDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _creditService.AddCreditCardAsync(tenantId, dto);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPut("edit/{tenantId}/{cardId}")]
        public async Task<IActionResult> EditCreditCard(long tenantId, long cardId, [FromForm] EditCreditCardDto dto)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var result = await _creditService.EditCreditCardAsync(tenantId, cardId, dto);

                return Ok(result);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("all/{userId}")]
        public async Task<IActionResult> GetAll(long userId)
        {
            try
            {
                var cards = await _creditService.GetAllCardsAsync(userId);
                return Ok(cards);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet("{userId}/{cardId}")]
        public async Task<IActionResult> GetOne(long userId, long cardId)
        {
            try
            {
                var card = await _creditService.GetCreditCardByIdAsync(userId, cardId);
                return Ok(card);
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
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

        [HttpDelete("delete/{tenantId}/{cardId}")]
        public async Task<IActionResult> DeleteCreditCard(long tenantId, long cardId)
        {
            try
            {
                var result = await _creditService.DeleteCreditCardAsync(tenantId, cardId);
                return Ok(new { message = "Credit card deleted successfully." });
            }
            catch (KeyNotFoundException ex)
            {
                return NotFound(new { error = ex.Message });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }


    }
}
