using otherServices.Models.Enums;

namespace WebAPIDotNet.DTOs
{
    public class RegisterResponseDTO
    {
        public long UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public UserRole Role { get; set; }
        public int FlagWaitingUser { get; set; }
        public string NIDPath { get; set; }
        public string FileName { get; set; }

    }
}

