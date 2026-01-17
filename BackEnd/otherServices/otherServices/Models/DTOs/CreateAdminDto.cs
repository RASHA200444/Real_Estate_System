using otherServices.Models.DTOs.DataAnnotations;
using otherServices.Models.Enums;
using System.ComponentModel.DataAnnotations;

namespace otherServices.Models.DTOs
{
    public class CreateAdminDto
    {
        [Username]
        [Required(ErrorMessage = "UserName is Required.")]
        public string UserName { get; set; }



        [Required(ErrorMessage = "Email is Required.")] 
        [EmailAddress(ErrorMessage ="Unvaild Email Address")]
        public string Email { get; set; }

        [Password]
        [Required(ErrorMessage = "Password is Required.")]
        public string Password { get; set; }
        [Required(ErrorMessage = "Password is Required.")]
        public string ConfirmPassword { get; set; }


        [Required(ErrorMessage = "PrivilegeType is Required.")]
        public AdminPrivilageType PrivilegeType { get; set; }
    }

}
