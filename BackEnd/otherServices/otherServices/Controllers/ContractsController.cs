using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otherServices.Models.Enums;
using otherServices.Services.Contracts;

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

        // POST: api/contracts/{id}/sign?role=Buyer&signerUserId=1
        [HttpPost("{contractId}/sign")]
        [Authorize]
        public async Task<IActionResult> Sign(long contractId, [FromQuery] SignerRole role, [FromQuery] long signerUserId)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
            var ua = Request.Headers.UserAgent.ToString();

            var res = await _service.SignAsync(contractId, signerUserId, role, ip, ua);
            return Ok(res);
        }
    }
}
