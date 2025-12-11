using FluentValidation;
using otherServices.Models.DTOs;
using otherServices.Models.DTOs.DataAnnotations;
using System.ComponentModel.DataAnnotations;

namespace otherServices.Models.DTOs
{
    public class UpdatePasswordDto
    {
        [Required(ErrorMessage = "OldPassword is Required")]
        public string OldPassword { get; set; }

        [Password]
        [Required(ErrorMessage = "NewPassword is Required")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "ConfirmPassword is Required")]
        public string ConfirmPassword { get; set; }

    }
}


public class UpdatePasswordValidator : AbstractValidator<UpdatePasswordDto>
{
    public UpdatePasswordValidator()
    {
        RuleFor(x => x.NewPassword)
            .Must((dto, newPassword) => newPassword != dto.OldPassword)
                .WithMessage("New password cannot be the same as the old password.");
    }
}
