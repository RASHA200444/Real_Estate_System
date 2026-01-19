using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otherServices.Models.Enums;
using otherServices.Services.Contracts;
using System.Security.Claims;

namespace otherServices.Controllers.Contracts
{
    [Route("api/contracts")]
    [ApiController]
    public class ContractsController : ControllerBase
    {
        private readonly IContractService _service;

        public ContractsController(IContractService service)
        {
            _service = service;
        }

        // POST: api/contracts/{id}/sign?role=Buyer
        [HttpPost("{contractId}/sign")]
        [Authorize]
        public async Task<IActionResult> Sign(long contractId, [FromQuery] SignerRole role)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var ua = Request.Headers.UserAgent.ToString();

            var signerUserIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("uid");
            if (string.IsNullOrWhiteSpace(signerUserIdStr) || !long.TryParse(signerUserIdStr, out var signerUserId))
                return Unauthorized(new { success = false, message = "Invalid token (missing userId)" });

            var res = await _service.SignAsync(contractId, signerUserId, role, ip, ua);
            return Ok(res);
        }

        // ✅ optional: verify endpoint
        [HttpGet("{contractId}/verify")]
        public async Task<IActionResult> Verify(long contractId)
        {
            var res = await _service.VerifyAsync(contractId);
            return Ok(res);
        }
    }
}
