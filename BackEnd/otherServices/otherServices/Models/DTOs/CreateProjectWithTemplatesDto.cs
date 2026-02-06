using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;
using otherServices.Models.Enums;
using System.Text.Json;

namespace otherServices.Models.DTOs.Projects
{
    public class CreateProjectWithTemplatesDto
    {
        [Required]
        public long CompanyId { get; set; }

        [Required]
        public string ProjectName { get; set; } = null!;

        [Required]
        public string Description { get; set; } = null!;

        [Required]
        public string Location { get; set; } = null!;

        public string? LocationPath { get; set; }

        [Required]
        public IFormFile ProjectDocFile { get; set; } = null!;

        [Required]
        public int TotalFloors { get; set; }

        [Required]
        public bool HasElevator { get; set; }

        [Required]
        public int UnitsPerFloor { get; set; }

        [Required]
        public PropertyType Type { get; set; }

        [Required]
        public string UnitTemplatesJson { get; set; } = null!;

        // ✅ NEW: Project-level tags (recommended)
        // Example: ["new-cairo","compound","project:Skyline","company:ABC"]
        public List<string>? Tags { get; set; }
    }
}
