using otherServices.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace otherServices.Models;

public class Company
{
    [Key, ForeignKey(nameof(User))]
    public long UserId { get; set; }

    public string CompanyName { get; set; } = null!;

    public string? CommercialRegisterPath { get; set; }
    public AIDecision CommercialRegisterEvaluation { get; set; } = AIDecision.Uncertain;

    public PendingStatus PendingStatus { get; set; } = PendingStatus.Pending;

    // Link to landlord
    public long LandlordId { get; set; }
    public Landlord Landlord { get; set; } = null!;

    // ✅ NEW (Module 5: Anomaly Detection for companies)
    public double? AnomalyScore { get; set; }
    public string? AnomalyReason { get; set; }
    public DateTime? AnomalyFlaggedAt { get; set; }

    public User User { get; set; } = null!;
    public ICollection<Project> Projects { get; set; } = new List<Project>();
}
