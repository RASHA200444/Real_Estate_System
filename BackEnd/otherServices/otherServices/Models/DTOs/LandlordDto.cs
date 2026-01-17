namespace otherServices.Models.DTOs
{
    public class LandlordDto
    {
        public long UserId { get; set; }
        public long LandlordId { get; set; }

        public string UserName { get; set; }
        public string Email { get; set; }
        public string RoleName { get; set; }

        public string ProfilePhotoPath { get; set; }
        public string NIDPath { get; set; }
        public int NIDEvaluation { get; set; }

        public string OwnershipDocPath { get; set; }
        public int OwnershipDocPathEvaluation { get; set; }

        public int PendingStatus { get; set; }
        public bool IsPro { get; set; }
        public int ComPanStatus { get; set; }

        public int Rate { get; set; }

    }
}
