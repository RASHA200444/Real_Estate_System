using otherServices.Models.Enums;

namespace otherServices.Models.DTOs
{
    public class AdminDto
    {
        public long UserId { get; set; }
        public long AdminId { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public AdminType Type { get; set; }
        public AdminPrivilageType PrivilegeType { get; set; }

    }

}
