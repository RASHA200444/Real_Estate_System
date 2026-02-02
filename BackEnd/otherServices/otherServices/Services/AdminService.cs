using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json; 
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using otherServices.Models;
using otherServices.Models.DTOs;
using otherServices.Models.DTOs.Posts;
using otherServices.Models.Enums;
using otherServices.Repositories;

namespace otherServices.Services
{
    public class AdminService : IAdminService
    {
        #region
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
        #endregion



        #region Posts
        public async Task AcceptPost(long postId)
        {
            await _postRepository.AcceptPostAsync(postId);
        }

        public async Task RejectPost(long postId)
        {
            await _postRepository.RejectPostAsync(postId);
        }

        public async Task<IEnumerable<WaitingPostsDto>> GetWaitingPosts()
        {
            var posts = await _postRepository.NestedFind(
                p => p.PendingStatus == PostPendingStatus.Pending
                     && p.PostDocPathEvaluation == AIDecision.Uncertain,
                p => p.Landlord,
                p => p.Landlord.User,
                p => p.PostImages
            );

            if (!posts.Any())
                throw new KeyNotFoundException("No waiting posts found.");

            return posts.Select(p => new WaitingPostsDto
            {
                PostId = p.PostId,

                UserId = p.Landlord.UserId,
                UserName = p.Landlord.User.UserName,
                Email = p.Landlord.User.Email,

                Title = p.Title,
                Description = p.Description,
                Price = (double)p.Price,

                Location = p.Location,
                LocationPath = p.LocationPath,
                PostDocPath = p.PostDocPath,

                NumberOfRooms = p.NumberOfRooms,
                NumberOfBathrooms = p.NumberOfBathrooms,
                Area = p.Area,
                TotalUnitsInBuilding = p.TotalUnitsInBuilding,

                IsFurnished = p.IsFurnished,
                HasGarage = p.HasGarage,
                FloorNumber = p.FloorNumber,

                StartRentalDate = p.StartRentalDate,
                EndRentalDate = p.EndRentalDate,
                DatePost = p.CreatedAt,

                RentalStatus = p.Status,
                RentType = p.Type,

                Images = p.PostImages
                            .Select(img => img.ImageUrl)
                            .ToList()
            }).ToList();
        }

        public async Task<IEnumerable<AllPostsDto>> GetPostsAsync()
        {
            var query = _postRepository.GetAllQueryable()
                .Where(p => p.Status != PropertyStatus.Sold)
                .Select(p => new AllPostsDto
                {
                    PostId = p.PostId,

                    UserId = p.Landlord.UserId,
                    UserName = p.Landlord.User.UserName,

                    Title = p.Title,
                    Description = p.Description,
                    Price = (double)p.Price,
                    Status = p.Status,

                    DatePost = p.CreatedAt,

                    Images = p.PostImages
                        .Select(img => img.ImageUrl)
                        .ToList()
                });

            var posts = await query.ToListAsync();

            if (!posts.Any())
                throw new KeyNotFoundException("No posts found.");

            return posts;
        }




        //public async Task<IEnumerable<PostSummaryDto>> GetPostsAsync()
        //{
        //    var posts = await _postRepository.NestedFind(
        //        p => p.Status != PropertyStatus.Sold,
        //        // && p.PendingStatus == PostPendingStatus.Accepted,
        //        p => p.Landlord,
        //        p => p.Landlord.User,
        //        p => p.PostImages
        //    );

        //    if (!posts.Any())
        //        throw new KeyNotFoundException("No posts found.");

        //    return posts.Select(MapToPostSummaryDto).ToList();
        //}

        //private PostSummaryDto MapToPostSummaryDto(Post p)
        //{
        //    return new PostSummaryDto
        //    {
        //        PostId = p.PostId,

        //        UserId = p.Landlord?.UserId ?? 0,
        //        UserName = p.Landlord?.User?.UserName ?? "Unknown",

        //        Title = p.Title,
        //        Description = p.Description,
        //        Price = p.Price,

        //        DatePost = p.CreatedAt,

        //        Images = p.PostImages?
        //                    .Select(img => img.ImageUrl)
        //                    .ToList()
        //                 ?? new List<string>()
        //    };
        //}

        #endregion


        #region Users

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
                //NIDPath = u.NIDPath,
                //NIDEvaluation = u.NIDEvaluation,
                CreatedAt = u.CreatedAt
            });
        }

        // =========================
        // Landlord approval (existing)
        // =========================
        public async Task AcceptUser(long userId)
        {
            await _landlordRepository.AcceptUserAsync(userId);
        }

        public async Task RejectUser(long userId)
        {
            await _landlordRepository.RejectUserAsync(userId);
        }

        public async Task<IEnumerable<WaitingLandlordsDto>> GetWaitingLandlord()
        {
            var users = await _context.Landlords
                .Include(l => l.User)
                .Where(l => l.User.RoleName == UserRole.Landlord)
                .Where(l => l.PendingStatus == PendingStatus.Pending)
                .Where(l => l.OwnershipDocPathEvaluation == AIDecision.Uncertain)
                .ToListAsync();

            if (!users.Any())
                throw new Exception("No Waiting Landlords");

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
            var Landlords = await _landlordRepository.FindAsync(p => p.UserId == userid);
            if (!Landlords.Any())
                throw new Exception("No Waiting Landlords");
            return Landlords;
        }

        // =========================
        // Company approval (NEW)
        // =========================
        public async Task<IEnumerable<CompanyDto>> GetWaitingCompanies()
        {
            var companies = await _context.Companies
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
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == companyUserId);
            if (company == null) throw new KeyNotFoundException("Company not found");

            company.PendingStatus = PendingStatus.Active;
            company.CommercialRegisterEvaluation = AIDecision.Verified;

            var publisher = await _context.Landlords.FirstOrDefaultAsync(l => l.LandlordId == company.LandlordId);
            if (publisher != null)
            {
                publisher.PendingStatus = PendingStatus.Active;
                publisher.OwnershipDocPathEvaluation = AIDecision.Verified;
            }

            await _context.SaveChangesAsync();
            return company;
        }

        public async Task<Company> RejectCompany(long companyUserId)
        {
            var company = await _context.Companies.FirstOrDefaultAsync(c => c.UserId == companyUserId);
            if (company == null) throw new KeyNotFoundException("Company not found");

            company.PendingStatus = PendingStatus.Blocked;
            company.CommercialRegisterEvaluation = AIDecision.Fraudulent;

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
                CreatedAt = p.CreatedAt,

                // ✅ NEW
                Tags = ParseTagsJson(p.TagsJson)
            });
        }

        public async Task<ProjectResponseDto> AcceptProject(long projectId)
        {
            var project = await _context.Projects
                .Include(p => p.UnitTemplates)
                .Include(p => p.Company)
                .FirstOrDefaultAsync(p => p.ProjectId == projectId);

            if (project == null)
                throw new KeyNotFoundException("Project not found");

            if (project.Company.PendingStatus != PendingStatus.Active)
                throw new Exception("Company not approved yet");

            if (project.UnitTemplates == null || !project.UnitTemplates.Any())
                throw new Exception("Project has no unit templates");

            if (project.PendingStatus == ProjectPendingStatus.Accepted)
            {
                return new ProjectResponseDto
                {
                    ProjectId = project.ProjectId,
                    ProjectName = project.ProjectName,
                    Location = project.Location,
                    PendingStatus = project.PendingStatus,
                    Tags = ParseTagsJson(project.TagsJson) // ✅ NEW
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
                PendingStatus = project.PendingStatus,
                Tags = ParseTagsJson(project.TagsJson) // ✅ NEW
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
            var company = project.Company;
            var publisherLandlordId = company.LandlordId;

            var publisher = await _context.Landlords.FirstOrDefaultAsync(l => l.LandlordId == publisherLandlordId);
            if (publisher == null) throw new Exception("Company publisher landlord not found");

            if (publisher.PendingStatus != PendingStatus.Active)
                throw new Exception("Company not active");

            // ✅ project-level tags
            var projectTags = ParseTagsJson(project.TagsJson);

            for (int floor = 1; floor <= project.TotalFloors; floor++)
            {
                foreach (var t in project.UnitTemplates)
                {
                    var price = t.BasePrice + ((floor - 1) * t.PriceIncreasePerFloor);

                    // ✅ post-level tags
                    var postTags = new List<string>
                    {
                        $"project:{project.ProjectName}",
                        $"company:{company.CompanyName}",
                        $"unit:{t.UnitCode}",
                        $"floor:{floor}",
                        $"rooms:{t.NumberOfRooms}",
                        project.Type == PropertyType.Sale ? "sale" : "rent",
                        project.Location
                    };

                    var mergedTags = NormalizeTags(projectTags.Concat(postTags));

                    var post = new Post
                    {
                        LandlordId = publisherLandlordId,

                        Title = $"{project.ProjectName} - Unit {t.UnitCode} - Floor {floor}",
                        Description = t.Description,
                        Price = price,

                        Location = project.Location,
                        LocationPath = project.LocationPath,
                        PostDocPath = project.ProjectDocPath,

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

                        PendingStatus = PostPendingStatus.Accepted,
                        PostDocPathEvaluation = AIDecision.Verified,

                        ProjectId = project.ProjectId,

                        // ✅ NEW: store tags on each post
                        // IMPORTANT: Post must have TagsJson property
                        TagsJson = ToTagsJson(mergedTags)
                    };

                    _context.Posts.Add(post);
                }
            }
        }



        // =========================
        // ✅ Tags Helpers
        // =========================
        private static List<string> ParseTagsJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json)) return new List<string>();
            try
            {
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch
            {
                return new List<string>();
            }
        }

        private static IEnumerable<string> NormalizeTags(IEnumerable<string> tags)
        {
            return tags
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase);
        }

        private static string ToTagsJson(IEnumerable<string> tags)
        {
            var clean = NormalizeTags(tags).ToList();
            return JsonSerializer.Serialize(clean);
        }
        #endregion
    }
}
