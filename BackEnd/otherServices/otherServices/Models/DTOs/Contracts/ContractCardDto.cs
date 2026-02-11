using System;

namespace otherServices.Models.DTOs.Contracts
{
    public class ContractCardDto
    {
        public long ContractId { get; set; }

        public string Type { get; set; } = string.Empty;   // "Rent" / "SaleCash" / "SaleInstallment"
        public string Status { get; set; } = string.Empty; // "PendingSellerSignature" / ...

        public DateTime CreatedAt { get; set; }

        public ContractOtherPartyDto OtherParty { get; set; } = new();

        public ContractCardPropertyDto Property { get; set; } = new();

        public ContractCardSignaturesDto Signatures { get; set; } = new();

        public ContractCardUiDto Ui { get; set; } = new();
    }

    public class ContractOtherPartyDto
    {
        public long UserId { get; set; }
        public string Name { get; set; } = string.Empty;

        public string Phone { get; set; } = string.Empty;
    }

    public class ContractCardPropertyDto
    {
        public long PostId { get; set; }

        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;   // "Rent" / "Sale"

        public int Rooms { get; set; }
        public int Baths { get; set; }
        public double Area { get; set; }

        public double? Price { get; set; }
    }

    public class ContractCardSignaturesDto
    {
        public bool BuyerSigned { get; set; }
        public bool SellerSigned { get; set; }
    }

    public class ContractCardUiDto
    {
        public string MyRole { get; set; } = string.Empty; // "Buyer" / "Seller"
        public string NextAction { get; set; } = "NONE";   // "SIGN" / "FINALIZE" / "WAIT_OTHER_PARTY" / "NONE"
    }
}
