using System;

namespace otherServices.Models.DTOs.Contracts
{
    public class ContractDetailsResponseDto
    {
        public bool Success { get; set; }

        public ContractHeaderDto Contract { get; set; } = new();

        public ContractPartiesDto Parties { get; set; } = new();

        public ContractPropertyDto Property { get; set; } = new();

        public ContractPaymentPlanDto? PaymentPlan { get; set; }   // ✅ NEW

        public ContractSignaturesDto Signatures { get; set; } = new();

        public ContractUiDto Ui { get; set; } = new();
    }

    public class ContractHeaderDto
    {
        public long ContractId { get; set; }
        public long? ProposalId { get; set; }
        public long PostId { get; set; }

        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        public string ContractHash { get; set; } = string.Empty;

        public int Version { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class ContractPartiesDto
    {
        public ContractPartyDto Tenant { get; set; } = new();
        public ContractPartyDto Landlord { get; set; } = new();
    }

    public class ContractPartyDto
    {
        public long UserId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
    }

    public class ContractPropertyDto
    {
        public long PostId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public string Type { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;

        public string Location { get; set; } = string.Empty;

        public double? Price { get; set; }
        public bool IsAuction { get; set; }

        public int NumberOfRooms { get; set; }
        public int NumberOfBathrooms { get; set; }
        public double Area { get; set; }

        public int? TotalUnitsInBuilding { get; set; }

        public bool IsFurnished { get; set; }
        public bool HasGarage { get; set; }
        public int? FloorNumber { get; set; }
    }

    // ✅ NEW SECTION
    public class ContractPaymentPlanDto
    {
        public string PlanType { get; set; } = string.Empty;
        // "Rent" / "SaleInstallment" (SaleCash = null)

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }

        public int? DurationMonths { get; set; }

        public int? IntervalMonths { get; set; }
        public string? FrequencyLabel { get; set; }
        // Monthly / Quarterly / SemiAnnual / Annual

        public int? PaymentsCount { get; set; }

        public decimal? PeriodicAmount { get; set; }
        public decimal? TotalAmount { get; set; }

        public decimal PlatformFeePercent { get; set; }

        public string Status { get; set; } = string.Empty;
    }

    public class ContractSignaturesDto
    {
        public ContractSignatureStateDto Buyer { get; set; } = new();
        public ContractSignatureStateDto Seller { get; set; } = new();
    }

    public class ContractSignatureStateDto
    {
        public bool Signed { get; set; }
        public DateTime? SignedAt { get; set; }
        public long? SignatureId { get; set; }
    }

    public class ContractUiDto
    {
        public string MyRole { get; set; } = string.Empty;

        public bool CanSign { get; set; }
        public bool CanFinalize { get; set; }

        public string NextAction { get; set; } = "NONE";
    }
}
