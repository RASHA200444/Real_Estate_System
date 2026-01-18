using otherServices.Models.Enums;

namespace otherServices.Models.DTOs
{
    public class ProposalDto
    {
        public long ProposalId { get; set; }
        public long PostId { get; set; }
        public string Title { get; set; }
        public string ImagePath { get; set; }

        public long LandlordId { get; set; }
        public long LandlordUserId { get; set; }
        public string? LandlordName { get; set; }
        public long TenantId { get; set; }
        public string? TenantName { get; set; }
        public string Phone { get; set; }
        public DateTime? StartRentalDate { get; set; }
        public DateTime? EndRentalDate { get; set; }
        public ProposalStatus ProposalStatus { get; set; } //  Rejected = -1, Waiting = 0, Approved = 1,
        public IsInstallment IsInstallment { get; set; } // Cash , Installment
        public string FilePath { get; set; }
        public double OfferedPrice { get; set; }

        //public string FileBase64 { get; set; }
    }
}