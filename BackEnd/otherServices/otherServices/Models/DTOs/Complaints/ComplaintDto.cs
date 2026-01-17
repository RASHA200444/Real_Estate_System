using otherServices.Models.Enums;

namespace otherServices.Models.DTOs.Complaints
{
    public class ComplaintDto
    {
        public long ComplaintId { get; set; }
        public long ReporterUserId { get; set; }
        public long ReportedUserId { get; set; }
        public string ReporterName { get; set; }
        public string ReportedName { get; set; }
        public ComplaintType Type { get; set; }
        public string Content { get; set; }
        public string? ImagePath { get; set; } // ✅ الصورة
        public ComplaintStatus? Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
