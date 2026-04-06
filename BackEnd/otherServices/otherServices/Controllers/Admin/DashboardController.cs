// ===============================
// File: otherServices/Controllers/Admin/DashboardController.cs
// ===============================
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otherServices.Services.Interfaces;

namespace otherServices.Controllers.Admin
{
    [Route("api/admin/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // حماية لوحة التحكم: للأدمن فقط
    public class DashboardController : BaseApiController // توحيد الوراثة
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            try
            {
                // حتى لو مش محتاجين الـ UserId في الميثود دي حالياً
                // استخدام RequireUserId بيضمن إن التوكن سليم وصاحبه موجود
                if (RequireUserId(out _) is IActionResult error) return error;

                var stats = await _dashboardService.GetDashboardAsync();
                return Ok(stats);
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}