using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otherServices.Models.DTOs;
using otherServices.Models.DTOs.Projects;
using otherServices.Services.Interfaces;
using System.Security.Claims;

namespace otherServices.Controllers
{
    [Route("api/company/projects")]
    [ApiController]
    public class CompanyProjectsController : ControllerBase
    {
        private readonly ICompanyProjectService _service;

        public CompanyProjectsController(ICompanyProjectService service)
        {
            _service = service;
        }

        [HttpPost("create")]
        public async Task<IActionResult> Create([FromForm] CreateProjectWithTemplatesDto dto)
        {
            var result = await _service.CreateProjectWithTemplates(dto);
            return Ok(result);
        }


        // ✅ NEW: delete project (delete only available posts; delete project+templates only if no sold/underNegotiation)
        [HttpDelete("{projectId:long}")]
        //[Authorize(Roles = "Company")]
        public async Task<IActionResult> Delete(long projectId)
        {
            var uidStr = User.FindFirstValue("uid");
            if (string.IsNullOrWhiteSpace(uidStr) || !long.TryParse(uidStr, out var companyUserId))
                return Unauthorized("Invalid token (uid missing).");

            var result = await _service.DeleteProject(companyUserId, projectId);
            return Ok(result);
        }

    }
}
