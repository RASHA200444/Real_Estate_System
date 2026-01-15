using otherServices.Models.Enums;

namespace otherServices.Models.DTOs
{
    public class ProjectDto
    {
        public long ProjectId { get; set; }
        public long CompanyId { get; set; }
        public string CompanyName { get; set; } = null!;

        public string ProjectName { get; set; } = null!;
        public string Description { get; set; } = null!;

        public string Location { get; set; } = null!;
        public string LocationPath { get; set; } = null!;
        public string ProjectDocPath { get; set; } = null!;

        public int TotalFloors { get; set; }
        public bool HasElevator { get; set; }
        public int UnitsPerFloor { get; set; }

        public PropertyType Type { get; set; }
        public ProjectPendingStatus PendingStatus { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
