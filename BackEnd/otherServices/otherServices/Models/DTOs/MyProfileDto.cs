namespace otherServices.Models.DTOs
{
    public class MyProfileDto
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public string ProfilePhotoPath { get; set; }
        public string NIDPath { get; set; }
        public string OwnershipDocumentPath { get; set; }

        // ✅ NEW
        public bool IsPro { get; set; }
        public bool TwoFactorEnabled { get; set; }

    }
}
