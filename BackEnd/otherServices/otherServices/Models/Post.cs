using otherServices.Models.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace otherServices.Models;

public partial class Post
{
    public long PostId { get; set; }
    public long LandlordId { get; set; }
    public string Title { get; set; } 
    public string Description { get; set; } 
    public double Price { get; set; }
    public string Location { get; set; } 
    public string LocationPath { get; set; }
    public string PostDocPath { get; set; }

    // Apartment specifications
    public int NumberOfRooms { get; set; }
    public int NumberOfBathrooms { get; set; }
    public double Area { get; set; }
    public int? TotalUnitsInBuilding { get; set; } // nullable

    // Additional optional attributes
    public bool IsFurnished { get; set; }
    public bool HasGarage { get; set; }
    public int? FloorNumber { get; set; }

    // Dates
    [Column(TypeName = "date")]
    public DateTime? StartRentalDate { get; set; }

    [Column(TypeName = "date")]
    public DateTime? EndRentalDate { get; set; }

    [Required]
    [Column(TypeName = "date")]
    public DateTime CreatedAt { get; set; }

    // Enums
    public PropertyType Type { get; set; } // Rent / Sale
    public PropertyStatus Status { get; set; } = PropertyStatus.Available; // Available / Sold / UnderNegotiation
    public PostPendingStatus PendingStatus { get; set; } = PostPendingStatus.Pending; // refused , Pending , Accepted
    public PriceEvaluation PriceEvaluation { get; set; } = PriceEvaluation.Acceptable; // VeryLow = -2, Low = -1, Acceptable = 0, High = 1, VeryHigh = 2
    public AIDecision PostDocPathEvaluation { get; set; } = AIDecision.Uncertain;  // NotReviewed = 0, Verified = 1, Fraudulent = 2, Uncertain = 3  




    public Landlord Landlord { get; set; }
    public ICollection<PostImage> PostImages { get; set; }
    public ICollection<Transaction> Transactions { get; set; }
    public ICollection<Comment> Comments { get; set; }
    public virtual ICollection<SavedPost> SavedPosts { get; set; } = new List<SavedPost>();

    //public virtual ICollection<User> Tenants { get; set; } = new List<User>();
    public ICollection<Proposal> Proposals { get; set; }
    public ICollection<Like> Likes { get; set; }

}
