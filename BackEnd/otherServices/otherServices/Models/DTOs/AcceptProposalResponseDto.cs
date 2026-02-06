using otherServices.Models.Enums;

namespace otherServices.Models.DTOs
{

        public class AcceptProposalResponseDto
        {
            public long ProposalId { get; set; }
            public long PostId { get; set; }
            public long TenantId { get; set; }
            public long LandlordUserId { get; set; }

            public PropertyType PropertyType { get; set; }
            public bool IsAuction { get; set; }
            public double? FinalPrice { get; set; }

            // for frontend routing
            public string NextAction { get; set; } = "INITIATE_PAYMENT_FLOW";
            public string? InitiateEndpoint { get; set; } // e.g. "/api/payments/sale/cash/{tenantId}"
            public string? SuggestedFlow { get; set; }     // "SALE_CASH" | "SALE_INSTALLMENT" | "RENT_START"
        }
    

}
