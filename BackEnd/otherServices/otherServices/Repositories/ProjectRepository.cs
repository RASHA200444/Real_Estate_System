//using otherServices.Models;
//using otherServices.Models.Enums;
//using otherServices.Repositories;

//public class ProjectRepository : GenericRepository<Project>, IProjectRepository
//{
//    private readonly AppDbContext2 _context;

//    public ProjectRepository(AppDbContext2 context) : base(context)
//    {
//        _context = context;
//    }

//    public async Task<Project> AcceptProjectAsync(long projectId)
//    {
//        var project = await _context.Projects.FindAsync(projectId)
//            ?? throw new KeyNotFoundException("Project not found");

//        project.PendingStatus = ProjectPendingStatus.Accepted;
//        await _context.SaveChangesAsync();

//        return project;
//    }

//    public async Task<Project> RejectProjectAsync(long projectId)
//    {
//        var project = await _context.Projects.FindAsync(projectId)
//            ?? throw new KeyNotFoundException("Project not found");

//        project.PendingStatus = ProjectPendingStatus.Rejected;
//        await _context.SaveChangesAsync();

//        return project;
//    }
//}
using otherServices.Models;
using otherServices.Models.Enums;

namespace otherServices.Repositories
{
    public class ProjectRepository : GenericRepository<Project>, IProjectRepository
    {
        public ProjectRepository(AppDbContext2 context) : base(context) { }

        public async Task<Project> AcceptProjectAsync(long projectId)
        {
            var project = await GetByIdAsync(projectId);
            if (project == null) throw new KeyNotFoundException("Project not found");

            project.PendingStatus = ProjectPendingStatus.Accepted;
            await SaveChangesAsync();
            return project;
        }

        public async Task<Project> RejectProjectAsync(long projectId)
        {
            var project = await GetByIdAsync(projectId);
            if (project == null) throw new KeyNotFoundException("Project not found");

            project.PendingStatus = ProjectPendingStatus.Rejected;
            await SaveChangesAsync();
            return project;
        }
    }
}
