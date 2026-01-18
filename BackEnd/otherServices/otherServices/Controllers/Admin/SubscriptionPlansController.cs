using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otherServices.Models.DTOs.Subscriptions;
using otherServices.Services.Admins;
using otherServices.Services.Interfaces.Admins;

namespace otherServices.Controllers.Admin
{
    [ApiController]
    [Route("api/subscription-plans")]

    public class SubscriptionPlansController : ControllerBase
    {
        private readonly ISubscriptionPlanService _service;

        public SubscriptionPlansController(ISubscriptionPlanService service)
        {
            _service = service;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetSubscriptionPlanById(long id)
        {
            try
            {
                var plan = await _service.GetSubscriptionPlanByIdAsync(id);

                if (plan == null)
                    return NotFound(new { message = $"Subscription plan with id {id} not found." });

                return Ok(plan);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllSubscriptionPlans()
        {
            try
            {
                var plans = await _service.GetAllSubscriptionPlansAsync();
                return Ok(plans);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AddSubscriptionPlan([FromForm] AddSubscriptionPlanDto dto)
        {
            try
            {
                var plan = await _service.AddSubscriptionPlanAsync(dto);
                return CreatedAtAction(nameof(GetSubscriptionPlanById), new { id = plan.Id }, plan);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Something went wrong." });
            }
        }

        [Authorize(Roles = "Admin")]
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSubscriptionPlan(long id, [FromBody] UpdateSubscriptionPlanDto dto)
        {
            try
            {
                var updatedPlan = await _service.UpdateSubscriptionPlanAsync(id, dto);

                if (updatedPlan == null)
                    return NotFound(new { message = $"Subscription plan with id {id} not found." });

                return Ok(updatedPlan);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Something went wrong." });
            }
        }
    }
}
