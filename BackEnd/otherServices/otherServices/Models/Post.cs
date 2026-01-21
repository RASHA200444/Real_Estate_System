using otherServices.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace otherServices.Models;

public partial class Post
{
    public long PostId { get; set; }
    public long LandlordId { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }

    public double? Price { get; set; }                     // ✅ nullable
    public bool IsAuction { get; set; } = false;           // ✅ NEW

    public string Location { get; set; }
    public string LocationPath { get; set; }
    public string PostDocPath { get; set; }

    // Apartment specifications
    public int NumberOfRooms { get; set; }
    public int NumberOfBathrooms { get; set; }
    public double Area { get; set; }
    public int? TotalUnitsInBuilding { get; set; }

    public bool IsFurnished { get; set; }
    public bool HasGarage { get; set; }
    public int? FloorNumber { get; set; }

    [Column(TypeName = "date")]
    public DateTime? StartRentalDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime? EndRentalDate { get; set; }

    [Required]
    [Column(TypeName = "date")]
    public DateTime CreatedAt { get; set; }

    public PropertyType Type { get; set; }
    public PropertyStatus Status { get; set; } = PropertyStatus.Available;
    public PostPendingStatus PendingStatus { get; set; } = PostPendingStatus.Pending;

    public PriceEvaluation PriceEvaluation { get; set; } = PriceEvaluation.Acceptable;
    public AIDecision PostDocPathEvaluation { get; set; } = AIDecision.Uncertain;

    public Landlord Landlord { get; set; }
    public ICollection<PostImage> PostImages { get; set; }
    public ICollection<Transaction> Transactions { get; set; }
    public ICollection<Comment> Comments { get; set; }
    public virtual ICollection<SavedPost> SavedPosts { get; set; } = new List<SavedPost>();
    public ICollection<Proposal> Proposals { get; set; }
    public ICollection<Like> Likes { get; set; }

    public long? ProjectId { get; set; }
    public Project Project { get; set; }

    public string? TagsJson { get; set; }
}
