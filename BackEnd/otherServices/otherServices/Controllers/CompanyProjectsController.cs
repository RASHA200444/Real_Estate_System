using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using otherServices.Models.DTOs.Projects;
using otherServices.Services.Interfaces;

namespace otherServices.Controllers
{
    [Route("api/company/projects")]
    [ApiController]
    [Authorize(Roles = "Company")]
    public class CompanyProjectsController : BaseApiController
    {
        private readonly ICompanyProjectService _service;

        public CompanyProjectsController(ICompanyProjectService service)
        {
            _service = service;
        }

        [HttpPost("create")]
        [Consumes("multipart/form-data")]

        public async Task<IActionResult> Create([FromForm] CreateProjectWithTemplatesDto dto)
        {
            // الـ Validation بيحصل أوتوماتيك بفضل الـ [ApiController] والـ FluentValidation
            var result = await _service.CreateProjectWithTemplates(dto);
            return Ok(result);
        }

        [HttpDelete("{projectId:long}")]
        public async Task<IActionResult> Delete(long projectId)
        {
            if (RequireUserId(out var companyUserId) is IActionResult error)
                return error;

            var result = await _service.DeleteProject(companyUserId, projectId);
            return Ok(result);
        }

        [HttpGet("my-projects")]
        public async Task<IActionResult> GetMyProjects()
        {
            if (RequireUserId(out var companyUserId) is IActionResult error)
                return error;

            // بنستخدم ProjectDto اللي عندك لعرض القائمة
            var result = await _service.GetProjectsByCompany(companyUserId);
            return Ok(result);
        }

        [HttpGet("{projectId:long}")]
        public async Task<IActionResult> GetProjectDetails(long projectId)
        {
            if (RequireUserId(out var companyUserId) is IActionResult error)
                return error;

            // بنستخدم ProjectDto مع إضافة لستة الـ Posts (الوحدات)
            var result = await _service.GetProjectDetailsForCompany(companyUserId, projectId);
            return Ok(result);
        }
    }
}