using otherServices.Models.DTOs.DataAnnotations;
using otherServices.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace otherServices.Models.DTOs
{
    public class UpdateAdminDto
    {
        [Username]
        public string? UserName { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        [Password]
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }
        public AdminPrivilageType? PrivilegeType { get; set; }
    }

}
