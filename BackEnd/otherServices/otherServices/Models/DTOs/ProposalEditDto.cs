using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using otherServices.Models.Enums;

namespace otherServices.Models.DTOs
{
    public class ProposalEditDto
    {
        [FromForm]
        public string? Phone { get; set; }

        [FromForm]
        public DateTime? StartRentalDate { get; set; }

        [FromForm]
        public DateTime? EndRentalDate { get; set; }

        [FromForm]
        public IFormFile? File { get; set; }

        [FromForm]
        public IsInstallment? IsInstallment { get; set; }

        [FromForm]
        public double? Offeredprice { get; set; }
    }

    public class ProposalEditDtoValidator : AbstractValidator<ProposalEditDto>
    {
        public ProposalEditDtoValidator()
        {
            When(x => !string.IsNullOrEmpty(x.Phone), () =>
            {
                RuleFor(x => x.Phone)
                    .Matches(@"^(01[0125][0-9]{8})$")
                    .WithMessage("Invalid Egyptian phone number.");
            });

            When(x => x.Offeredprice.HasValue, () =>
            {
                RuleFor(x => x.Offeredprice.Value)
                    .GreaterThan(0)
                    .WithMessage("Offered price must be > 0.");
            });

            When(x => x.File != null, () =>
            {
                RuleFor(x => x.File!)
                    .Must(f => f.Length > 0).WithMessage("File cannot be empty.")
                    .Must(f => IsValidFileType(f.FileName)).WithMessage("Only PDF, JPG, PNG files are allowed.")
                    .Must(f => f.Length <= 5 * 1024 * 1024).WithMessage("File size must be less than 5MB.");
            });

            When(x => x.StartRentalDate.HasValue && x.EndRentalDate.HasValue, () =>
            {
                RuleFor(x => x.EndRentalDate)
                    .GreaterThan(x => x.StartRentalDate)
                    .WithMessage("EndRentalDate must be later than StartRentalDate.");
            });
        }

        private static bool IsValidFileType(string fileName)
        {
            string[] permittedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(fileName).ToLower();
            return permittedExtensions.Contains(ext);
        }
    }
}
