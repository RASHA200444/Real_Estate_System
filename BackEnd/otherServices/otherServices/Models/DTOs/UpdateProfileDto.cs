namespace otherServices.Models.DTOs
{
    public class UpdateProfileDto
    {
        public string? FullName { get; set; }
        //public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public IFormFile? ProfilePhoto { get; set; }
        public IFormFile? NIDFile { get; set; }
    }
}
