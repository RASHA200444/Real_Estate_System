using System.ComponentModel.DataAnnotations;

namespace otherServices.Models.DTOs
{
    public class LoginDTO
    {
        [Required(ErrorMessage = "Username or email is required")]
        public string UsernameOrEmail { get; set; }

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }
    }
}
