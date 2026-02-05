using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using otherServices.Services.Interfaces;

namespace otherServices.Controllers.Admin
{
    [Route("api/admin/[controller]")]
    [ApiController]
    public class DashboardController : ControllerBase
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet]
        public async Task<IActionResult> GetDashboard()
        {
            try { 
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
