using otherServices.Models.Enums; 
namespace otherServices.Models
{
    public class Complaint
    {
        public long ComplaintId { get; set; }

        public long ReporterUserId { get; set; }
        public long ReportedUserId { get; set; } // ممكن يتحدد لاحقاً بناءً على اليوزرنيم
        //public string ReportedUserName { get; set; }

        public ComplaintType Type { get; set; }
        public string Content { get; set; }
        public string? ImagePath { get; set; } // ✅ صورة الشكوى


        public ComplaintStatus? Status { get; set; } = ComplaintStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public User ReporterUser { get; set; }
        public User ReportedUser { get; set; }
    }
}
