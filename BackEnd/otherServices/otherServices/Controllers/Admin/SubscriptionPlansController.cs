// ===============================
// File: otherServices/Controllers/Admin/SubscriptionPlansController.cs
// ===============================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otherServices.Models.DTOs.Subscriptions;
using otherServices.Services.Interfaces.Admins;

namespace otherServices.Controllers.Admin
{
    [ApiController]
    [Route("api/subscription-plans")]
    public class SubscriptionPlansController : BaseApiController // توحيد الوراثة
    {
        private readonly ISubscriptionPlanService _service;

        public SubscriptionPlansController(ISubscriptionPlanService service)
        {
            _service = service;
        }

        // ✅ مسموح للكل يشوف الخطط (عشان يختاروا هيشتركوا في إيه)
        [HttpGet("{id:long}")]
        [AllowAnonymous]
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
        [AllowAnonymous]
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

        // ❌ الإضافة للأدمن فقط
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AddSubscriptionPlan([FromForm] AddSubscriptionPlanDto dto)
        {
            try
            {
                // نضمن وجود الـ UserId للأدمن في سجلاتنا لو الخدمة بتحتاجه
                if (RequireUserId(out var adminId) is IActionResult error) return error;

                var plan = await _service.AddSubscriptionPlanAsync(dto);
                return CreatedAtAction(nameof(GetSubscriptionPlanById), new { id = plan.Id }, plan);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Something went wrong while adding the plan." });
            }
        }

        // ❌ التعديل للأدمن فقط
        [HttpPut("{id:long}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateSubscriptionPlan(long id, [FromBody] UpdateSubscriptionPlanDto dto)
        {
            try
            {
                if (RequireUserId(out _) is IActionResult error) return error;

                var updatedPlan = await _service.UpdateSubscriptionPlanAsync(id, dto);
                if (updatedPlan == null)
                    return NotFound(new { message = $"Subscription plan with id {id} not found." });

                return Ok(updatedPlan);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return StatusCode(500, new { message = "Something went wrong while updating the plan." });
            }
        }
    }
}