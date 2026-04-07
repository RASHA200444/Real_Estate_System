// ===============================
// File: otherServices/Controllers/Contracts/ContractsController.cs
// ===============================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otherServices.Models.Enums;
using otherServices.Services.Contracts;

namespace otherServices.Controllers.Contracts
{
    [Route("api/contracts")]
    [ApiController]
    public class ContractsController : BaseApiController // الوراثة من الكلاس الجديد
    {
        private readonly IContractService _service;

        public ContractsController(IContractService service)
        {
            _service = service;
        }

        // ✅ GET: api/contracts/my
        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> MyContracts()
        {
            // سطر واحد بديل لكل العك اللي فات
            if (RequireUserId(out var userId) is IActionResult error) return error;

            var res = await _service.GetMyContractsAsync(userId);
            return Ok(res);
        }

        // ✅ GET: api/contracts/{contractId}
        [HttpGet("{contractId:long}")]
        [Authorize]
        public async Task<IActionResult> Get(long contractId)
        {
            if (RequireUserId(out var userId) is IActionResult error) return error;

            var res = await _service.GetContractForUserAsync(contractId, userId);

            if (!res.Success)
                return Forbid();

            return Ok(res);
        }

        // POST: api/contracts/{id}/sign?role=Buyer|Seller
        [HttpPost("{contractId:long}/sign")]
        [Authorize]
        public async Task<IActionResult> Sign(long contractId, [FromQuery] SignerRole role)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var ua = Request.Headers.UserAgent.ToString();

            if (RequireUserId(out var signerUserId) is IActionResult error) return error;

            var res = await _service.SignAsync(contractId, signerUserId, role, ip, ua);
            return Ok(res);
        }

        // optional: verify endpoint
        [HttpGet("{contractId:long}/verify")]
        public async Task<IActionResult> Verify(long contractId)
        {
            // دي مش محتاجة Authorize غالباً فمش محتاجة UserId
            var res = await _service.VerifyAsync(contractId);
            return Ok(res);
        }
    }
}