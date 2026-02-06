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

        // ✅ GET: api/contracts/my
        [HttpGet("my")]
        [Authorize]
        public async Task<IActionResult> MyContracts()
        {
            var userId = ExtractUserId();
            if (userId == null)
                return Unauthorized(new { success = false, message = "Invalid token (missing userId)" });

            var res = await _service.GetMyContractsAsync(userId.Value);
            return Ok(res);
        }

        // ✅ GET: api/contracts/{contractId} (secure details)
        [HttpGet("{contractId}")]
        [Authorize]
        public async Task<IActionResult> Get(long contractId)
        {
            var userId = ExtractUserId();
            if (userId == null)
                return Unauthorized(new { success = false, message = "Invalid token (missing userId)" });

            var res = await _service.GetContractForUserAsync(contractId, userId.Value);
            return Ok(res);
        }

        // POST: api/contracts/{id}/sign?role=Buyer
        [HttpPost("{contractId}/sign")]
        [Authorize]
        public async Task<IActionResult> Sign(long contractId, [FromQuery] SignerRole role)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var ua = Request.Headers.UserAgent.ToString();

            var signerUserId = ExtractUserId();
            if (signerUserId == null)
                return Unauthorized(new { success = false, message = "Invalid token (missing userId)" });

            var res = await _service.SignAsync(contractId, signerUserId.Value, role, ip, ua);
            return Ok(res);
        }

        // optional: verify endpoint
        [HttpGet("{contractId}/verify")]
        public async Task<IActionResult> Verify(long contractId)
        {
            var res = await _service.VerifyAsync(contractId);
            return Ok(res);
        }

        private long? ExtractUserId()
        {
            var userIdStr =
                User.Claims.FirstOrDefault(c => c.Type == "uid")?.Value
                ?? User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                ?? User.Claims.FirstOrDefault(c => c.Type.EndsWith("/nameidentifier"))?.Value;

            if (string.IsNullOrWhiteSpace(userIdStr) || !long.TryParse(userIdStr, out var userId))
                return null;

            return userId;
        }
    }
}
