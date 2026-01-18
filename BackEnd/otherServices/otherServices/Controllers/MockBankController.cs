using Microsoft.AspNetCore.Mvc;
using otherServices.Models.DTOs.Payments;
using otherServices.Services.Payments;

namespace otherServices.Controllers.Payments
{
    [Route("api/mock-bank")]
    [ApiController]
    public class MockBankController : ControllerBase
    {
        private readonly IMockBankCardVault _vault;

        public MockBankController(IMockBankCardVault vault)
        {
            _vault = vault;
        }

        // زرع كارت + رصيد داخل البنك (اختبار)
        [HttpPost("seed-card")]
        public async Task<IActionResult> Seed([FromBody] SeedBankCardRequestDto dto)
        {
            try
            {
                var id = await _vault.SeedBankCardAsync(dto);
                return Ok(new { bankCardId = id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.InnerException?.Message ?? ex.Message });
            }
        }

        // خصم (في الموك دلوقتي لازم cvv + token + amount)
        [HttpPost("charge")]
        public async Task<IActionResult> Charge([FromBody] ChargeCardRequestDto dto)
        {
            try
            {
                var res = await _vault.ChargeAsync(dto.CardToken, dto.CVV, dto.Amount);
                return Ok(res);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.InnerException?.Message ?? ex.Message });
            }
        }
    }
}
