
using otherServices.Models.Enums;
using System.ComponentModel.DataAnnotations;
using FluentValidation;
using otherServices.Models.DTOs.DataAnnotations;

namespace otherServices.Models.DTOs
{
    public class RegisterDTO
    {
        //[Username]
        [Required(ErrorMessage = "Username is required")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; }

        [Password]
        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }

        [Required(ErrorMessage = "ConfirmPassword is required")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Role_name is required")]
        public UserRole Role_name { get; set; }

        // ✅ يبقى اختياري عشان Company
        public IFormFile? NIDFile { get; set; }

        // landlord ownership doc
        public IFormFile? File { get; set; }

        // ✅ Company fields (NEW)
        public string? CompanyName { get; set; }
        public IFormFile? CommercialRegisterFile { get; set; }
    }

    public class RegisterDTOValidator : AbstractValidator<RegisterDTO>
    {
        public RegisterDTOValidator()
        {
            RuleFor(x => x.UserName)
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

            // ✅ Tenant/Landlord/Company conditional validations
            RuleFor(x => x.NIDFile)
                .NotNull()
                .When(x => x.Role_name != UserRole.Company)
                .WithMessage("NID File is required");

            RuleFor(x => x.File)
                .NotNull()
                .When(x => x.Role_name == UserRole.Landlord)
                .WithMessage("As a Landlord You should upload an ownership document");

            RuleFor(x => x.CompanyName)
                .NotEmpty()
                .When(x => x.Role_name == UserRole.Company)
                .WithMessage("CompanyName is required for Company registration");

            RuleFor(x => x.CommercialRegisterFile)
                .NotNull()
                .When(x => x.Role_name == UserRole.Company)
                .WithMessage("Commercial register document is required for Company registration");
        }
    }
}

