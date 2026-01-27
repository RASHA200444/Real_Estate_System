using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otherServices.Models.DTOs.Payments;
using otherServices.Services.Payments.Eligibility;
using System.Security.Claims;

namespace otherServices.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "TenantPolicy")]
    public class EligibilityController : ControllerBase
    {
        private readonly IEligibilityService _eligibility;

        public EligibilityController(IEligibilityService eligibility)
        {
            _eligibility = eligibility;
        }

        private long GetUserId()
        {
            var idStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(idStr, out var id)) throw new Exception("Invalid token user id");
            return id;
        }

        // ✅ Installment
        [HttpPost("installment/{proposalId:long}")]
        public async Task<IActionResult> EvaluateInstallment(long proposalId, [FromBody] EligibilityFormRequestDto dto)
        {
            var userId = GetUserId();
            var res = await _eligibility.EvaluateInstallmentAsync(userId, proposalId, dto);
            return Ok(res);
        }

        // ✅ Rent
        [HttpPost("rent/{proposalId:long}")]
        public async Task<IActionResult> EvaluateRent(long proposalId, [FromBody] EligibilityFormRequestDto dto)
        {
            var userId = GetUserId();
            var res = await _eligibility.EvaluateRentAsync(userId, proposalId, dto);
            return Ok(res);
        }

        // ✅ Read snapshot
        [HttpGet("{proposalId:long}")]
        public async Task<IActionResult> GetSnapshot(long proposalId)
        {
            var userId = GetUserId();
            var res = await _eligibility.GetEligibilitySnapshotAsync(userId, proposalId);
            return Ok(res);
        }
    }
}
