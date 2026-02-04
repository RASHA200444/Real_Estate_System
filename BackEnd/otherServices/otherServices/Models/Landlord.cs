using otherServices.Models.Enums;

namespace otherServices.Models
{
    public class Landlord
    {
        public long LandlordId { get; set; }

        public long UserId { get; set; }
        public User User { get; set; }

        public double Rate { get; set; } = 0;

        public string? OwnershipDocPath { get; set; }
        public AIDecision OwnershipDocPathEvaluation { get; set; }  // Verified/Fraudulent/Uncertain/NotReviewed

        public PendingStatus PendingStatus { get; set; } // Blocked - Pending - Active

        public bool IsPro { get; set; } = false;

        public ComPanStatus ComPanStatus { get; set; } = 0;
        public DateTime? SuspendedUntil { get; set; }

        // ✅ NEW (Module 5: Anomaly Detection for landlords)
        public double? AnomalyScore { get; set; }
        public string? AnomalyReason { get; set; }
        public DateTime? AnomalyFlaggedAt { get; set; }

        // Relation
        public ICollection<Post> Posts { get; set; }
        public ICollection<Rating> Ratings { get; set; }
    }
}
