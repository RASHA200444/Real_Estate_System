using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using otherServices.Models.Enums;

namespace otherServices.Models;

public partial class User
{
    public long UserId { get; set; }
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;

    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Password { get; set; } = null!;
    public DateTime? LastPassChange { get; set; }
    public UserRole RoleName { get; set; }  // Tenant, Owner, Admin
    public string? ProfilePhotoPath { get; set; }
    public string? NIDPath { get; set; }
    public AIDecision NIDEvaluation { get; set; } // NotReviewed = 0, Verified = 1, Fraudulent = 2, Uncertain = 3  
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ComPanStatus ComPanStatus { get; set; } = 0; // Free - Panned - Blocked
    public DateTime? SuspendedUntil { get; set; }


    // Relations
    public Admin? Admin { get; set; }
    public Landlord Landlord { get; set; }

    public ICollection<Comment> Comments { get; set; }
    [JsonIgnore]
    public virtual ICollection<Message> SentMessages { get; set; } = new List<Message>();
    [JsonIgnore]
    public virtual ICollection<Message> ReceivedMessages { get; set; } = new List<Message>();
    public virtual ICollection<SavedPost> SavedPosts { get; set; } = new List<SavedPost>();
    public ICollection<Proposal> Proposals { get; set; }
    public virtual ICollection<Like> Likes { get; set; } = new List<Like>();
    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<Rating> Ratings { get; set; } = new List<Rating>();
    public ICollection<Complaint> ComplaintsReported { get; set; }
    public ICollection<Complaint> ComplaintsAgainst { get; set; }
    public ICollection<Transaction> Transactions { get; set; }
    public List<CreditCard> CreditCards { get; set; } = new();
 }
