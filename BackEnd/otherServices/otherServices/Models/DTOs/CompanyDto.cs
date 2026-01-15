using otherServices.Models.Enums;

namespace otherServices.Models.DTOs
{
    public class CompanyDto
    {
        public long UserId { get; set; }
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;

        public string CompanyName { get; set; } = null!;
        public string? CommercialRegisterPath { get; set; }
        public AIDecision CommercialRegisterEvaluation { get; set; }
        public PendingStatus PendingStatus { get; set; }
    }
}
