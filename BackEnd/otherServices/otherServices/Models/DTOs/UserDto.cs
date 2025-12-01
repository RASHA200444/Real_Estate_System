using otherServices.Models.Enums;

namespace otherServices.Models.DTOs
{
    public class UserDto
    {
        public long UserId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Address { get; set; }
        public UserRole RoleName { get; set; }
        public AIDecision NIDEvaluation { get; set; }
        public DateTime CreatedAt { get; set; }
    }

}
