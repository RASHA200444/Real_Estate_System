using otherServices.Models.Enums;
using System;

namespace otherServices.Models
{
    public class Complaint
    {
        public long ComplaintId { get; set; }

        public long ReporterUserId { get; set; }
        public long ReportedUserId { get; set; }

        public ComplaintType Type { get; set; }
        public string Content { get; set; }
        public string? ImagePath { get; set; }

        public ComplaintStatus? Status { get; set; } = ComplaintStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // ✅ NEW: AI moderation/report analysis
        public int? AiSeverity { get; set; }
        public string? AiReason { get; set; }
        public DateTime? AiAssessedAt { get; set; }

        public User ReporterUser { get; set; }
        public User ReportedUser { get; set; }
    }
}
