using Microsoft.EntityFrameworkCore;
using otherServices.Models;
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

        public CompanyProjectService(AppDbContext2 context, IMediaService mediaService)
        {
            _context = context;
            _mediaService = mediaService;
        }

        public async Task<ProjectResponseDto> CreateProjectWithTemplates(CreateProjectWithTemplatesDto dto)
        {
            // 1) Load company + check approval
            var company = await _context.Companeis
                .Include(c => c.User)
                .FirstOrDefaultAsync(c => c.UserId == dto.CompanyId);

            if (company == null)
                throw new KeyNotFoundException("Company not found");

            if (company.PendingStatus != PendingStatus.Active)
                throw new Exception("Company not approved by admin yet");

            // 2) Project doc file (لازم يكون PDF / JPG / PNG حسب MediaService)
            if (dto.ProjectDocFile == null || dto.ProjectDocFile.Length == 0)
                throw new Exception("ProjectDocFile is required");

            string projectDocPath = await _mediaService.SaveFileAsync(dto.ProjectDocFile);

            // 3) Deserialize UnitTemplatesJson (robust)
            if (string.IsNullOrWhiteSpace(dto.UnitTemplatesJson))
                throw new Exception("UnitTemplatesJson is required");

            string raw = dto.UnitTemplatesJson.Trim();

            List<UnitTemplateDto>? templatesDto;

            try
            {
                // ✅ (A) لو جاية متغلفة Quotes:  "[{...}]"
                if (raw.StartsWith("\"") && raw.EndsWith("\""))
                {
                    raw = JsonSerializer.Deserialize<string>(raw) ?? "";
                    raw = raw.Trim();
                }

                // ✅ (B) لو جاية Escaped: [{\"unitCode\":\"A\"...}]
                if (raw.Contains("\\\""))
                {
                    raw = raw.Replace("\\\"", "\"");
                    raw = raw.Replace("\\n", "");
                    raw = raw.Replace("\\r", "");
                    raw = raw.Replace("\\t", "");
                }

                // ✅ (C) لو في أي كلام قبل/بعد: استخرج بين أول [ وآخر ]
                int start = raw.IndexOf('[');
                int end = raw.LastIndexOf(']');
                if (start >= 0 && end > start)
                    raw = raw.Substring(start, end - start + 1);

                templatesDto = JsonSerializer.Deserialize<List<UnitTemplateDto>>(
                    raw,
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                );
            }
            catch (Exception ex) when (ex is JsonException || ex is ArgumentException)
            {
                // 👇 هتطلعلك القيمة اللي واصلة فعلاً عشان تعرف سبب المشكلة فورًا
                string preview = dto.UnitTemplatesJson.Length > 400
                    ? dto.UnitTemplatesJson.Substring(0, 400) + "..."
                    : dto.UnitTemplatesJson;

                throw new Exception("Invalid UnitTemplatesJson format. Preview: " + preview);
            }

            if (templatesDto == null || templatesDto.Count == 0)
                throw new Exception("UnitTemplates are required");

            // 4) Validate UnitsPerFloor
            if (dto.UnitsPerFloor <= 0)
                throw new Exception("UnitsPerFloor must be > 0");

            if (templatesDto.Count != dto.UnitsPerFloor)
                throw new Exception("UnitTemplates count must equal UnitsPerFloor");

            // 5) Validate UnitCode duplicates (A/B/C..)
            var duplicatedUnitCodes = templatesDto
                .GroupBy(t => (t.UnitCode ?? "").Trim().ToUpper())
                .Where(g => g.Count() > 1)
                .Select(g => g.Key)
                .ToList();

            if (duplicatedUnitCodes.Any())
                throw new Exception($"Duplicate UnitCode(s): {string.Join(", ", duplicatedUnitCodes)}");

            // 6) Create Project (Pending for admin)
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
                CreatedAt = DateTime.UtcNow
            };

            await _context.Projects.AddAsync(project);
            await _context.SaveChangesAsync(); // عشان ProjectId

            // 7) Map templates DTO -> Entity
            var templateEntities = templatesDto.Select(t => new UnitTemplate
            {
                ProjectId = project.ProjectId,
                UnitCode = (t.UnitCode ?? "").Trim(),

                Title = t.Title ?? "",
                Description = t.Description ?? "",

                NumberOfRooms = t.NumberOfRooms,
                NumberOfBathrooms = t.NumberOfBathrooms,
                Area = t.Area,

                IsFurnished = t.IsFurnished,
                HasGarage = t.HasGarage,

                BasePrice = t.BasePrice,
                PriceIncreasePerFloor = t.PriceIncreasePerFloor
            }).ToList();

            // ✅ لازم DbSet<UnitTemplate> UnitTemplates في AppDbContext2
            await _context.UnitTemplates.AddRangeAsync(templateEntities);
            await _context.SaveChangesAsync();

            return new ProjectResponseDto
            {
                ProjectId = project.ProjectId,
                ProjectName = project.ProjectName,
                Location = project.Location,
                PendingStatus = project.PendingStatus
            };

        }
    }
}
