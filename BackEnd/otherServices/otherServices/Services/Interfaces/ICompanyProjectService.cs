using otherServices.Models;
using otherServices.Models.DTOs;
using otherServices.Models.DTOs.Projects;

namespace otherServices.Services.Interfaces
{
    public interface ICompanyProjectService
    {
        Task<ProjectResponseDto> CreateProjectWithTemplates(CreateProjectWithTemplatesDto dto);
    }
}
