using Microsoft.AspNetCore.Mvc;
using otherServices.Models.DTOs.Payments;
using otherServices.Services.Payments;

namespace otherServices.Controllers.Payments
{
    [Route("api/payments/flow")]
    [ApiController]
    public class PaymentFlowController : ControllerBase
    {
        private readonly IPaymentFlowService _flow;

        public PaymentFlowController(IPaymentFlowService flow)
        {
            _flow = flow;
        }

        [HttpPost("buy/{userId}")]
        public async Task<IActionResult> Buy(long userId, [FromBody] BuyPostRequestDto dto)
            => Ok(await _flow.BuyPostAsync(userId, dto));

        [HttpPost("accept-proposal/{landlordUserId}")]
        public async Task<IActionResult> AcceptProposal(long landlordUserId, [FromBody] AcceptProposalPayRequestDto dto)
            => Ok(await _flow.AcceptProposalAndStartAsync(landlordUserId, dto));

        [HttpPost("pay-remaining")]
        public async Task<IActionResult> PayRemaining([FromBody] PayRemainingRequestDto dto)
            => Ok(await _flow.PayRemainingAsync(dto));
    }
}
