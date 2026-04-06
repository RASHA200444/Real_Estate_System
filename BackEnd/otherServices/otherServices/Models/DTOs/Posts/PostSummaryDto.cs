using otherServices.Models.Enums;

namespace otherServices.Models.DTOs.Posts
{
    public class PostSummaryDto
    {
        public long PostId { get; set; }

        public long UserId { get; set; }
        public string UserName { get; set; } = "";

        public string Title { get; set; } = "";
        public string Description { get; set; } = "";

        public double Price { get; set; }
        public bool IsAuction { get; set; } = false;

        public DateTime DatePost { get; set; }
        public List<string> Images { get; set; } = new();

        // ✅ حالات الإعلان
        public PostPendingStatus PendingStatus { get; set; }      // Pending / Accepted / Refused
        public PropertyStatus Status { get; set; }                // Available / UnderNegotiation / Sold
        public PropertyType Type { get; set; }                    // Rent / Sale

        // ✅ AI signals المهمة
        public AIDecision PostDocPathEvaluation { get; set; }      // Uncertain / Verified / Fraudulent / NotReviewed
        public PriceEvaluation PriceEvaluation { get; set; }       // VeryLow..VeryHigh

        public AIDecision? FakePropertyEvaluation { get; set; }
        public AIDecision? ImageManipulationEvaluation { get; set; }

        public double? AiConfidence { get; set; }
        public string? AiReason { get; set; }
        public DateTime? AiLastCheckedAt { get; set; }

        // ✅ Flags مريحة للـ UI
        public bool NeedsAdminReview { get; set; }   // يظهر في waiting list بتاعة اللاندلورد
        public bool HasAiResults { get; set; }       // هل AI رجّع أصلاً ولا لسه؟

        public int? FloorNumber { get; set; } // ضيف ده جوه كلاس PostSummaryDto
    }
}
