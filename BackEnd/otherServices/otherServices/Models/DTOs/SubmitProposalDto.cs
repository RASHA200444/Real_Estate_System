using FluentValidation;
using Microsoft.AspNetCore.Http;
using otherServices.Models.Enums;

namespace otherServices.Models.DTOs
{
    public class SubmitProposalDto
    {
        public string Phone { get; set; }
        public DateTime? StartRentalDate { get; set; }
        public DateTime? EndRentalDate { get; set; }

        public IsInstallment IsInstallment { get; set; }
        public double? Offeredprice { get; set; }

        public IFormFile File { get; set; }

        // ✅ NEW: eligibility form answers JSON string (sent with proposal)
        public string? EligibilityAnswersJson { get; set; }
    }

    public class SubmitProposalDtoValidator : AbstractValidator<SubmitProposalDto>
    {
        public SubmitProposalDtoValidator()
        {
            RuleFor(x => x.Phone)
                .NotEmpty().WithMessage("Phone number is required.")
                .Matches(@"^(01[0125][0-9]{8})$").WithMessage("Invalid Egyptian phone number.");

            When(x => x.Offeredprice.HasValue, () =>
            {
                RuleFor(x => x.Offeredprice!.Value)
                    .GreaterThan(0).WithMessage("Offered price must be > 0.");
            });

            RuleFor(x => x.File)
                .Cascade(CascadeMode.Stop)
                .NotNull().WithMessage("File is required.")
                .Must(f => f != null && f.Length > 0).WithMessage("File cannot be empty.")
                .Must(f => f == null || IsValidFileType(f.FileName)).WithMessage("Only PDF, JPG, PNG files are allowed.")
                .Must(f => f == null || f.Length <= 5 * 1024 * 1024).WithMessage("File size must be less than 5MB.");

            RuleFor(x => x.EndRentalDate)
                .GreaterThan(x => x.StartRentalDate)
                .When(x => x.StartRentalDate.HasValue && x.EndRentalDate.HasValue)
                .WithMessage("EndRentalDate must be later than StartRentalDate.");

            // EligibilityAnswersJson is validated in TenantService depending on post/type.
        }

        private static bool IsValidFileType(string fileName)
        {
            string[] permittedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(fileName).ToLower();
            return permittedExtensions.Contains(ext);
        }
    }
}
