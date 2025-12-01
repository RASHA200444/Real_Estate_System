using Microsoft.EntityFrameworkCore.Metadata.Internal;
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
        public AIDecision OwnershipDocPathEvaluation { get; set; }  // NotReviewed = 0, Verified = 1, Fraudulent = 2, Uncertain = 3  

        public PendingStatus PendingStatus { get; set; } //  Blocked - Pending - Active

        public bool IsPro { get; set; } = false;
        public bool IsBanned { get; set; } = false;
        public DateTime? SuspendedUntil { get; set; }

        // Relation
        public ICollection<Post> Posts { get; set; }
        public ICollection<Rating> Ratings { get; set; }

    }
}
