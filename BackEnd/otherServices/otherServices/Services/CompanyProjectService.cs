using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using otherServices.Models;
using otherServices.Models.DTOs;
using otherServices.Models.DTOs.Posts;
using otherServices.Models.DTOs.Projects;
using otherServices.Models.Enums;
using otherServices.Services.Interfaces;
using System.Text.Json;

namespace otherServices.Services
{
    public class CompanyProjectService : ICompanyProjectService
    {
        private readonly AppDbContext2 _context;
        private readonly IMediaService _mediaService;
        private readonly INotificationService _notificationService;
        public CompanyProjectService(AppDbContext2 context, IMediaService mediaService , INotificationService notificationService)
        {
            _context = context;
            _mediaService = mediaService;
            _notificationService = notificationService;
        }

        // 1. إنشاء المشروع (كما هو)
        public async Task<ProjectResponseDto> CreateProjectWithTemplates(CreateProjectWithTemplatesDto dto)
        {
            var company = await _context.Companies
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == dto.CompanyId);

            if (company == null) throw new KeyNotFoundException("Company not found");
            if (company.PendingStatus != PendingStatus.Active) throw new Exception("Company not approved yet");

            if (dto.ProjectDocFile == null || dto.ProjectDocFile.Length == 0) throw new Exception("ProjectDocFile is required");
            string projectDocPath = await _mediaService.SaveFileAsync(dto.ProjectDocFile);

            if (dto.UnitTemplates == null || !dto.UnitTemplates.Any()) throw new Exception("UnitTemplates are required");
            if (dto.UnitsPerFloor <= 0) throw new Exception("UnitsPerFloor must be > 0");
            if (dto.UnitTemplates.Count != dto.UnitsPerFloor) throw new Exception($"Templates count must match UnitsPerFloor ({dto.UnitsPerFloor})");

            var duplicatedUnitCodes = dto.UnitTemplates
                .GroupBy(t => (t.UnitCode ?? "").Trim().ToUpper())
                .Where(g => g.Count() > 1)
                .Select(g => g.Key).ToList();
            if (duplicatedUnitCodes.Any()) throw new Exception($"Duplicate UnitCode(s): {string.Join(", ", duplicatedUnitCodes)}");

            var project = new Project
            {
                CompanyId = dto.CompanyId,
                ProjectName = dto.ProjectName,
                Description = dto.Description,
                Location = dto.Location,
                LocationPath = dto.LocationPath ?? string.Empty,
                ProjectDocPath = projectDocPath,
                TotalFloors = dto.TotalFloors,
                HasElevator = dto.HasElevator,
                UnitsPerFloor = dto.UnitsPerFloor,
                Type = dto.Type,
                PendingStatus = ProjectPendingStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                TagsJson = dto.Tags != null ? JsonSerializer.Serialize(dto.Tags) : null
            };

            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync();

            var templateEntities = dto.UnitTemplates.Select(t => new UnitTemplate
            {
                ProjectId = project.ProjectId,
                UnitCode = t.UnitCode.Trim(),
                Title = t.Title,
                Description = t.Description,
                NumberOfRooms = t.NumberOfRooms,
                NumberOfBathrooms = t.NumberOfBathrooms,
                Area = t.Area,
                IsFurnished = t.IsFurnished,
                HasGarage = t.HasGarage,
                BasePrice = t.BasePrice,
                PriceIncreasePerFloor = t.PriceIncreasePerFloor
            }).ToList();

            await _context.UnitTemplates.AddRangeAsync(templateEntities);
            await _context.SaveChangesAsync();

            return new ProjectResponseDto
            {
                ProjectId = project.ProjectId,
                ProjectName = project.ProjectName,
                Location = project.Location,
                PendingStatus = project.PendingStatus,
                Tags = dto.Tags ?? new List<string>()
            };
        }

        // 2. حذف المشروع (كما هو)
		public async Task<DeleteProjectResultDto> DeleteProject(long companyUserId, long projectId)
		{
			var project = await _context.Projects
				.Include(p => p.Company)
				.FirstOrDefaultAsync(p => p.ProjectId == projectId && p.CompanyId == companyUserId);

			if (project == null)
				throw new KeyNotFoundException("Project not found for this company.");

			var userId = project.Company.UserId;
			var projectName = project.ProjectName;

			using var tx = await _context.Database.BeginTransactionAsync();

			var posts = await _context.Posts
				.Where(p => p.ProjectId == projectId)
				.ToListAsync();

			var availablePosts = posts
				.Where(p => p.Status == PropertyStatus.Available)
				.ToList();

			if (availablePosts.Any())
				_context.Posts.RemoveRange(availablePosts);

			var remaining = posts.Count(p => p.Status != PropertyStatus.Available);

			if (remaining > 0)
			{
				await _context.SaveChangesAsync();
				await tx.CommitAsync();

				return new DeleteProjectResultDto
				{
					ProjectId = projectId,
					DeletedAvailablePosts = availablePosts.Count,
					RemainingNonAvailablePosts = remaining,
					ProjectDeleted = false
				};
			}

			_context.Projects.Remove(project);
			await _context.SaveChangesAsync();
			await tx.CommitAsync();

			// ✅ الإشعار في المكان الصح
			await _notificationService.SendNotificationAsync(
				userId: userId,
				title: "تم حذف المشروع",
				content: $"تم حذف مشروعك '{projectName}' بنجاح.",
				type: NotificationType.PostDeactivated
			);

			return new DeleteProjectResultDto
			{
				ProjectId = projectId,
				DeletedAvailablePosts = availablePosts.Count,
				RemainingNonAvailablePosts = 0,
				ProjectDeleted = true
			};
		}

        // 3. جلب قائمة المشاريع (تم حل مشكلة الـ Tags)
        public async Task<List<ProjectDto>> GetProjectsByCompany(long companyUserId)
        {
            var projects = await _context.Projects
                .Where(p => p.CompanyId == companyUserId)
                .ToListAsync();

            return projects.Select(p => new ProjectDto
            {
                ProjectId = p.ProjectId,
                CompanyId = p.CompanyId,
                ProjectName = p.ProjectName,
                Description = p.Description,
                Location = p.Location,
                LocationPath = p.LocationPath,
                ProjectDocPath = p.ProjectDocPath,
                TotalFloors = p.TotalFloors,
                HasElevator = p.HasElevator,
                UnitsPerFloor = p.UnitsPerFloor,
                PendingStatus = p.PendingStatus,
                CreatedAt = p.CreatedAt,
                Tags = string.IsNullOrEmpty(p.TagsJson)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(p.TagsJson) ?? new List<string>()
            }).ToList();
        }

        // 4. جلب تفاصيل المشروع (تم حل مشكلة Return Type و FloorNumber و Casting)
        public async Task<CompanyProjectFullDetailsDto> GetProjectDetailsForCompany(long companyUserId, long projectId)
        {
            var projectEntity = await _context.Projects
                .FirstOrDefaultAsync(p => p.ProjectId == projectId && p.CompanyId == companyUserId);

            if (projectEntity == null) throw new KeyNotFoundException("Project not found.");

            // تحويل الـ Entity لـ DTO
            var projectDto = new ProjectDto
            {
                ProjectId = projectEntity.ProjectId,
                CompanyId = projectEntity.CompanyId,
                ProjectName = projectEntity.ProjectName,
                Description = projectEntity.Description,
                Location = projectEntity.Location,
                LocationPath = projectEntity.LocationPath,
                ProjectDocPath = projectEntity.ProjectDocPath,
                TotalFloors = projectEntity.TotalFloors,
                HasElevator = projectEntity.HasElevator,
                UnitsPerFloor = projectEntity.UnitsPerFloor,
                PendingStatus = projectEntity.PendingStatus,
                CreatedAt = projectEntity.CreatedAt,
                Tags = string.IsNullOrEmpty(projectEntity.TagsJson)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(projectEntity.TagsJson) ?? new List<string>()
            };

            var units = await _context.Posts
                .Where(p => p.ProjectId == projectId)
                .Select(p => new PostSummaryDto
                {
                    PostId = p.PostId,
                    UserId = p.LandlordId,
                    Title = p.Title,
                    Description = p.Description,
                    Price = p.Price ?? 0, 
                    Status = p.Status,
                    PendingStatus = p.PendingStatus,
                    Type = p.Type,
                    DatePost = p.CreatedAt,
                    HasAiResults = p.AiLastCheckedAt != null,
                    PriceEvaluation = p.PriceEvaluation,
                    PostDocPathEvaluation = p.PostDocPathEvaluation,
                    AiConfidence = p.AiConfidence,
                    AiReason = p.AiReason,
                    AiLastCheckedAt = p.AiLastCheckedAt,
                    FloorNumber = p.FloorNumber,
                    FakePropertyEvaluation = p.FakePropertyEvaluation,
                    ImageManipulationEvaluation = p.ImageManipulationEvaluation
                })
                .ToListAsync();

            return new CompanyProjectFullDetailsDto
            {
                Project = projectDto,
                Units = units
            };
        }
    }
}