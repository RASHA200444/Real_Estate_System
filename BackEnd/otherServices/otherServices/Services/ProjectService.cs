using otherServices.Models;
using otherServices.Models.Enums;
using otherServices.Repositories;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepo;

    public ProjectService(IProjectRepository projectRepo)
    {
        _projectRepo = projectRepo;
    }

    public async Task<Project> CreateProject(CreateProjectDto dto)
    {
        var project = new Project
        {
            CompanyId = dto.CompanyId,
            ProjectName = dto.ProjectName,
            Description = dto.Description,
            Location = dto.Location,
            PendingStatus = ProjectPendingStatus.Pending
        };

        await _projectRepo.AddAsync(project);
        return project;
    }
}
