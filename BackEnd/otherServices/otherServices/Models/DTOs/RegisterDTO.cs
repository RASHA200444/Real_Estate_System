using otherServices.Models.Enums;
using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace otherServices.Models.DTOs
{
    public class RegisterDTO
    {
        public string Username { get; set; }


        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; }

        public string Password { get; set; }


        [Required(ErrorMessage = "ConfirmPassword is required")]
        public string ConfirmPassword { get; set; }


        [Required(ErrorMessage = "Role_name is required")]
        public UserRole Role_name { get; set; }


        [Required(ErrorMessage = "NID File is required")]
        public IFormFile NIDFile { get; set; }

        public IFormFile? File { get; set; }


    }


    public class RegisterDTOValidator : AbstractValidator<RegisterDTO>
    {
        public RegisterDTOValidator()
        {

            RuleFor(x => x.Username)
                .NotEmpty().WithMessage("Username is required.")
                .MinimumLength(3).WithMessage("Username must be at least 3 characters.")
                .MaximumLength(20).WithMessage("Username must not exceed 20 characters.")
                .Matches("^[a-zA-Z0-9]+(-[a-zA-Z0-9]+)*$")
                .WithMessage("Username may only contain letters, numbers, or single hyphens, and cannot begin or end with a hyphen.");



            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("New password is required.")
                .MinimumLength(8).WithMessage("Password must be at least 8 characters long.")
                .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
                .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
                .Matches("[0-9]").WithMessage("Password must contain at least one number.")
                .Matches("[^a-zA-Z0-9]").WithMessage("Password must contain at least one special character (!@#$%^&* etc).")
                .Must((dto, newPassword) => newPassword == dto.ConfirmPassword)
                    .WithMessage("password must equal ConfirmPassword.");
        }
    }
 
}



