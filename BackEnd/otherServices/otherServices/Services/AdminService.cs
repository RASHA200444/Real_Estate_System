using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using otherServices.Models;
using otherServices.Models.DTOs;
using otherServices.Models.Enums;
using otherServices.Repositories;

namespace otherServices.Services
{
    public class AdminService : IAdminService
    {
        private readonly IUserRepository _userRepository;
        private readonly ILandlordRepository _landlordRepository;
        private readonly IPostRepository _postRepository;
        private readonly IProjectRepository _projectRepository;
        private readonly IWebHostEnvironment _env;
        private readonly AppDbContext2 _context;

        public AdminService(
            IUserRepository userRepository,
            ILandlordRepository landlordRepository,
            IPostRepository postRepository,
            IWebHostEnvironment env,
            AppDbContext2 context,
            IProjectRepository projectRepository)
        {
            _landlordRepository = landlordRepository;
            _postRepository = postRepository;
            _env = env;
            _context = context;
            _userRepository = userRepository;
            _projectRepository = projectRepository;
        }

        // =========================
        // Posts approval
        // =========================
        public async Task<Post> AcceptPost(long postId)
        {
            return await _postRepository.AcceptPostAsync(postId);
        }

        public async Task<Post> RejectPost(long postId)
        {
            return await _postRepository.RejectPostAsync(postId);
        }

        public async Task<IEnumerable<PostDTo>> GetWaitingPosts()
        {
            var posts = await _postRepository.NestedFind(
                p => p.PendingStatus == PostPendingStatus.Pending,
                p => p.Landlord,
                p => p.PostImages,
                p => p.Landlord.User
            );

            posts = posts.Where(p => p.PostDocPathEvaluation == AIDecision.Uncertain);

            return posts.Select(p => new PostDTo
            {
                UserId = p.Landlord.UserId,
                //LandlordId = p.Landlord.LandlordId,
                UserName = p.Landlord.User.UserName,
                Email = p.Landlord.User.Email,

                PostId = p.PostId,
                Title = p.Title,
                Description = p.Description,
                Price = p.Price,
                PriceEvaluation = p.PriceEvaluation,
                Location = p.Location,
                LocationPath = p.LocationPath,
                RentalStatus = p.Status,
                DatePost = p.CreatedAt,
                FlagWaitingPost = p.PendingStatus,
                PostDocPathEvaluation = p.PostDocPathEvaluation,

                Images = p.PostImages.Select(pi => pi.ImageUrl).ToList(),
                PostDocPath = p.PostDocPath,

                NumOfRooms= p.NumberOfRooms,
                NumOfBathrooms = p.NumberOfBathrooms,
                Area = p.Area,
                IsFurnished = p.IsFurnished,
                HasGarage = p.HasGarage,
                FloorNumber = p.FloorNumber,
                RentType = p.Type,
                StartRentalDate = p.StartRentalDate,
                EndRentalDate = p.EndRentalDate

            });
        }

        // =========================
        // Landlord approval (existing)
        // =========================
        public async Task<Landlord> AcceptUser(long userId)
        {
            return await _landlordRepository.AcceptUserAsync(userId);
        }

        public async Task<Landlord> RejectUser(long userId)
        {
            return await _landlordRepository.RejectUserAsync(userId);
        }

        public async Task<IEnumerable<WaitingLandlordsDto>> GetWaitingLandlord()
        {
            var users = await _context.Landlords
                .Include(l => l.User)
                .Where(l => l.User.RoleName == UserRole.Landlord)
                .Where(l => l.PendingStatus == PendingStatus.Pending)
                .Where(l => l.OwnershipDocPathEvaluation == AIDecision.Uncertain)
                .ToListAsync();

            return users.Select(p => new WaitingLandlordsDto
            {
                UserId = p.UserId,
                LandlordId = p.LandlordId,
                UserName = p.User.UserName,
                Email = p.User.Email,
                OwnershipDocPath = p.OwnershipDocPath,
                OwnershipDocPathEvaluation = p.OwnershipDocPathEvaluation,
                NIDPath = p.User.NIDPath,
                NIDEvaluation = p.User.NIDEvaluation
            });
        }

        public async Task<IEnumerable<Landlord>> GetLandlordStatus(long userid)
        {
            return await _landlordRepository.FindAsync(p => p.UserId == userid);
        }

        // =========================
        // Company approval (NEW)
        // =========================
        public async Task<IEnumerable<CompanyDto>> GetWaitingCompanies()
        {
            var companies = await _context.Companeis
                .Include(c => c.User)
                .Where(c => c.User.RoleName == UserRole.Company)
                .Where(c => c.PendingStatus == PendingStatus.Pending)
                .Where(c => c.CommercialRegisterEvaluation == AIDecision.Uncertain)
                .ToListAsync();

            return companies.Select(c => new CompanyDto
            {
                UserId = c.UserId,
                UserName = c.User.UserName,
                Email = c.User.Email,
                CompanyName = c.CompanyName,
                CommercialRegisterPath = c.CommercialRegisterPath,
                CommercialRegisterEvaluation = c.CommercialRegisterEvaluation,
                PendingStatus = c.PendingStatus
            });
        }

        public async Task<Company> AcceptCompany(long companyUserId)
        {
            var company = await _context.Companeis.FirstOrDefaultAsync(c => c.UserId == companyUserId);
            if (company == null) throw new KeyNotFoundException("Company not found");

            company.PendingStatus = PendingStatus.Active;
            company.CommercialRegisterEvaluation = AIDecision.Verified;

            // كمان فعل landlord publisher المرتبط بالشركة
            var publisher = await _context.Landlords.FirstOrDefaultAsync(l => l.LandlordId == company.LandlordId);
            if (publisher != null)
            {
                publisher.PendingStatus = PendingStatus.Active;
                publisher.OwnershipDocPathEvaluation = AIDecision.Verified; // مجرد علامة (مفيش doc)
            }

            await _context.SaveChangesAsync();
            return company;
        }

        public async Task<Company> RejectCompany(long companyUserId)
        {
            var company = await _context.Companeis.FirstOrDefaultAsync(c => c.UserId == companyUserId);
            if (company == null) throw new KeyNotFoundException("Company not found");

            company.PendingStatus = PendingStatus.Blocked;
            company.CommercialRegisterEvaluation = AIDecision.Fraudulent;

            // اقفل publisher landlord
            var publisher = await _context.Landlords.FirstOrDefaultAsync(l => l.LandlordId == company.LandlordId);
            if (publisher != null)
            {
                publisher.PendingStatus = PendingStatus.Blocked;
                publisher.OwnershipDocPathEvaluation = AIDecision.Fraudulent;
            }

            await _context.SaveChangesAsync();
            return company;
        }

        // =========================
        // Project approval + Auto-create posts (NEW)
        // =========================
        public async Task<IEnumerable<ProjectDto>> GetWaitingProjects()
        {
            var projects = await _context.Projects
                .Include(p => p.Company)
                .Where(p => p.PendingStatus == ProjectPendingStatus.Pending)
                .ToListAsync();

            return projects.Select(p => new ProjectDto
            {
                ProjectId = p.ProjectId,
                CompanyId = p.CompanyId,
                CompanyName = p.Company.CompanyName,

                ProjectName = p.ProjectName,
                Description = p.Description,

                Location = p.Location,
                LocationPath = p.LocationPath,
                ProjectDocPath = p.ProjectDocPath,

                TotalFloors = p.TotalFloors,
                HasElevator = p.HasElevator,
                UnitsPerFloor = p.UnitsPerFloor,

                Type = p.Type,
                PendingStatus = p.PendingStatus,
                CreatedAt = p.CreatedAt
            });
        }

        public async Task<ProjectResponseDto> AcceptProject(long projectId)
        {
            // Load project + templates + company
            var project = await _context.Projects
                .Include(p => p.UnitTemplates)
                .Include(p => p.Company)
                .FirstOrDefaultAsync(p => p.ProjectId == projectId);

            if (project == null)
                throw new KeyNotFoundException("Project not found");

            // لازم الشركة تكون Active
            if (project.Company.PendingStatus != PendingStatus.Active)
                throw new Exception("Company not approved yet");

            // لازم UnitTemplates موجودة
            if (project.UnitTemplates == null || !project.UnitTemplates.Any())
                throw new Exception("Project has no unit templates");

            // لو اتقبل قبل كده، رجّع DTO وخلاص (أو ارفض)
            if (project.PendingStatus == ProjectPendingStatus.Accepted)
            {
                return new ProjectResponseDto
                {
                    ProjectId = project.ProjectId,
                    ProjectName = project.ProjectName,
                    Location = project.Location,
                    PendingStatus = project.PendingStatus
                };
            }

            project.PendingStatus = ProjectPendingStatus.Accepted;

            bool alreadyGenerated = await _context.Posts.AnyAsync(p => p.ProjectId == project.ProjectId);
            if (!alreadyGenerated)
                await CreatePostsFromProjectAsync(project);

            await _context.SaveChangesAsync();

            return new ProjectResponseDto
            {
                ProjectId = project.ProjectId,
                ProjectName = project.ProjectName,
                Location = project.Location,
                PendingStatus = project.PendingStatus
            };
        }

        public async Task<ProjectResponseDto> RejectProject(long projectId)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectId == projectId);

            if (project == null)
                throw new KeyNotFoundException("Project not found");

            project.PendingStatus = ProjectPendingStatus.Rejected;
            await _context.SaveChangesAsync();

            return new ProjectResponseDto
            {
                ProjectId = project.ProjectId,
                ProjectName = project.ProjectName,
                Location = project.Location,
                PendingStatus = project.PendingStatus
            };
        }


        private async Task CreatePostsFromProjectAsync(Project project)
        {
            // company publisher landlord id
            var company = project.Company;
            var publisherLandlordId = company.LandlordId;

            // ensure publisher exists + active
            var publisher = await _context.Landlords.FirstOrDefaultAsync(l => l.LandlordId == publisherLandlordId);
            if (publisher == null) throw new Exception("Company publisher landlord not found");

            if (publisher.PendingStatus != PendingStatus.Active)
                throw new Exception("Company not active");

            // for each floor and each template => create post
            for (int floor = 1; floor <= project.TotalFloors; floor++)
            {
                foreach (var t in project.UnitTemplates)
                {
                    // price logic
                    var price = t.BasePrice + ((floor - 1) * t.PriceIncreasePerFloor);

                    var post = new Post
                    {
                        LandlordId = publisherLandlordId,

                        Title = $"{project.ProjectName} - Unit {t.UnitCode} - Floor {floor}",
                        Description = t.Description,
                        Price = price,

                        Location = project.Location,
                        LocationPath = project.LocationPath,
                        PostDocPath = project.ProjectDocPath, // أو ملف خاص بالشقة لو عندك

                        NumberOfRooms = t.NumberOfRooms,
                        NumberOfBathrooms = t.NumberOfBathrooms,
                        Area = t.Area,
                        TotalUnitsInBuilding = project.TotalFloors * project.UnitsPerFloor,

                        IsFurnished = t.IsFurnished,
                        HasGarage = t.HasGarage,
                        FloorNumber = floor,

                        CreatedAt = DateTime.UtcNow,

                        Type = project.Type,
                        Status = PropertyStatus.Available,

                        // ✅ posts created after project approval => no need post approval again
                        PendingStatus = PostPendingStatus.Accepted,
                        PostDocPathEvaluation = AIDecision.Verified,

                        ProjectId = project.ProjectId
                    };

                    _context.Posts.Add(post);
                }
            }
        }

        // =========================
        // Users list
        // =========================
        public async Task<IEnumerable<UserDto>> GetUsers()
        {
            var users = await _userRepository.GetAllAsync();

            return users.Select(u => new UserDto
            {
                UserId = u.UserId,
                UserName = u.UserName,
                Email = u.Email,
                Phone = u.Phone,
                Address = u.Address,
                RoleName = u.RoleName,
                NIDPath = u.NIDPath,
                NIDEvaluation = u.NIDEvaluation,
                CreatedAt = u.CreatedAt
            });
        }
    }
}
