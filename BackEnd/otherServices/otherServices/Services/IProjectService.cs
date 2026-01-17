using otherServices.Models;

public interface IProjectService
{
    Task<Project> CreateProject(CreateProjectDto dto);
}
