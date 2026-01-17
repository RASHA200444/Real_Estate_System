using otherServices.Models.Enums;

namespace otherServices.Models.DTOs
{
    public class WaitingLandlordsDto
    {
        public long UserId { get; set; }
        public long LandlordId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string OwnershipDocPath { get; set; }
        public AIDecision OwnershipDocPathEvaluation { get; set; }  // NotReviewed = 0, Verified = 1, Fraudulent = 2, Uncertain = 3  

        public string NIDPath { get; set; }
        public AIDecision NIDEvaluation { get; set; } // NotReviewed = 0, Verified = 1, Fraudulent = 2, Uncertain = 3  


    }
}
