using Microsoft.AspNetCore.Mvc;
using otherServices.Models.DTOs;
using otherServices.Models.DTOs.Projects;
using otherServices.Services.Interfaces;

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

    }
}
