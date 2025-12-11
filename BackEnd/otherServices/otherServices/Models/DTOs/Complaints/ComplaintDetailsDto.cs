using otherServices.Models.Enums;

namespace otherServices.Models.DTOs.Complaints
{
    public class ComplaintDetailsDto
    {
        public long ComplaintId { get; set; }
        public long ReporterUserId { get; set; }
        public long ReportedUserId { get; set; }
        public string ReporterName { get; set; }
        public string ReportedName { get; set; }
        public string? ReporterPhone { get; set; }
        public string? ReporterEmail { get; set; }

        public ComplaintType Type { get; set; }
        public string Content { get; set; }
        public string? ImagePath { get; set; } 
        public ComplaintStatus? Status { get; set; }
        public DateTime CreatedAt { get; set; }

    }

}
