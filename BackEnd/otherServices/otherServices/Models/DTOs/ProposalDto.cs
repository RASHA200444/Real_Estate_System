using otherServices.Models.Enums;

namespace otherServices.Models.DTOs
{
    public class ProposalDto
    {
        public long ProposalId { get; set; }
        public long PostId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string ImagePath { get; set; }

        public long LandlordUserId { get; set; }
        public string? LandlordName { get; set; }

        public long TenantId { get; set; }
        public string? TenantName { get; set; }

        public string Phone { get; set; }

        public DateTime? StartRentalDate { get; set; }
        public DateTime? EndRentalDate { get; set; }

        public ProposalStatus ProposalStatus { get; set; }
        public IsInstallment IsInstallment { get; set; }

        public string FilePath { get; set; }
        public double OfferedPrice { get; set; }

        // ✅ AI readiness
        public AIInstallmentDecision IsAble { get; set; } = AIInstallmentDecision.NotCertain;
        public AIRentDecision RentIsAble { get; set; } = AIRentDecision.NotCertain;

        public int? EligibilityScore { get; set; }
        public string? EligibilityReason { get; set; }
        public DateTime? EligibilityAssessedAt { get; set; }

        public PropertyType PropertyType { get; set; } // Sale / Rent
        public bool IsAuction { get; set; }
    }
}
