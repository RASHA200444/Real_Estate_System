using otherServices.Models;

namespace otherServices.Repositories
{
    public interface IProjectRepository : IGenericRepository<Project>
    {
        Task<Project> AcceptProjectAsync(long projectId);
        Task<Project> RejectProjectAsync(long projectId);
    }
}
