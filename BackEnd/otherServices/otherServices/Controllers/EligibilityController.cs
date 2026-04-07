// ===============================
// File: otherServices/Controllers/EligibilityController.cs
// ===============================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otherServices.Models.DTOs.Payments;
using otherServices.Services.Payments.Eligibility;

namespace otherServices.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "TenantPolicy")]
    public class EligibilityController : BaseApiController // الوراثة من الكلاس الجديد
    {
        private readonly IEligibilityService _eligibility;

        public EligibilityController(IEligibilityService eligibility)
        {
            _eligibility = eligibility;
        }

        // ✅ Installment
        [HttpPost("installment/{proposalId:long}")]
        public async Task<IActionResult> EvaluateInstallment(long proposalId, [FromBody] EligibilityFormRequestDto dto)
        {
            if (RequireUserId(out var userId) is IActionResult error) return error;

            var res = await _eligibility.EvaluateInstallmentAsync(userId, proposalId, dto);
            return Ok(res);
        }

        // ✅ Rent
        [HttpPost("rent/{proposalId:long}")]
        public async Task<IActionResult> EvaluateRent(long proposalId, [FromBody] EligibilityFormRequestDto dto)
        {
            if (RequireUserId(out var userId) is IActionResult error) return error;

            var res = await _eligibility.EvaluateRentAsync(userId, proposalId, dto);
            return Ok(res);
        }

        // ✅ Read snapshot
        [HttpGet("{proposalId:long}")]
        public async Task<IActionResult> GetSnapshot(long proposalId)
        {
            if (RequireUserId(out var userId) is IActionResult error) return error;

            var res = await _eligibility.GetEligibilitySnapshotAsync(userId, proposalId);
            return Ok(res);
        }
    }
}