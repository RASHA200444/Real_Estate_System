using otherServices.Models.Enums;

namespace otherServices.Models
{
    public class Project
    {
        public long ProjectId { get; set; }
        public long CompanyId { get; set; }

        public string ProjectName { get; set; } = null!;
        public string Description { get; set; } = null!;

        public string Location { get; set; } = null!;
        public string LocationPath { get; set; } = null!;
        public string ProjectDocPath { get; set; } = null!;

        // ✅ NEW: Tags as JSON array string
        // Example: ["new-cairo","compound","project:Skyline","company:ABC"]
        public string? TagsJson { get; set; }

        // مواصفات عامة للمبنى
        public int TotalFloors { get; set; }              // عدد الأدوار
        public bool HasElevator { get; set; }             // فيه اسانسير؟
        public int UnitsPerFloor { get; set; }            // عدد الشقق في الدور

        // نوع العقار (Rent/Sale) زي Post
        public PropertyType Type { get; set; }

        public ProjectPendingStatus PendingStatus { get; set; } = ProjectPendingStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Nav
        public Company Company { get; set; } = null!;
        public ICollection<UnitTemplate> UnitTemplates { get; set; } = new List<UnitTemplate>();

        // الربط مع posts اللي اتعملت بعد القبول
        public ICollection<Post> Posts { get; set; } = new List<Post>();
    }
}
