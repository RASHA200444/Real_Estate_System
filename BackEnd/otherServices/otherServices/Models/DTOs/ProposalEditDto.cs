using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using otherServices.Models.Enums;
using FluentValidation;
using otherServices.Models.DTOs;
using System.IO;
using otherServices.Models.Enums;

namespace otherServices.Models.DTOs
{
    public class ProposalEditDto
    {
        [FromForm]
        public string? Phone { get; set; }  // nullable

        [FromForm]
        public DateTime? StartRentalDate { get; set; }

        [FromForm]
        public DateTime? EndRentalDate { get; set; }

        [FromForm]
        public IFormFile? File { get; set; } // nullable

        [FromForm]
        public IsInstallment? IsInstallment { get; set; } // nullable

        [FromForm]
        public double? Offeredprice { get; set; } // nullable
    }



public class ProposalEditDtoValidator : AbstractValidator<ProposalEditDto>
    {
        public ProposalEditDtoValidator()
        {
            // Phone validation (optional)
            When(x => !string.IsNullOrEmpty(x.Phone), () =>
            {
                RuleFor(x => x.Phone)
                    .Matches(@"^(01[0125][0-9]{8})$").WithMessage("Invalid Egyptian phone number.");
            });

            // Offeredprice validation (optional)
            When(x => x.Offeredprice.HasValue, () =>
            {
                RuleFor(x => x.Offeredprice)
                    .GreaterThan(0).WithMessage("Offered price must be greater than zero.");
            });

            // File validation (optional)
            When(x => x.File != null, () =>
            {
                RuleFor(x => x.File)
                    .Must(f => f.Length > 0).WithMessage("File cannot be empty.")
                    .Must(f => IsValidFileType(f.FileName)).WithMessage("Only PDF, JPG, PNG files are allowed.")
                    .Must(f => f.Length <= 5 * 1024 * 1024).WithMessage("File size must be less than 5MB.");
            });

            // Dates validation: EndRentalDate > StartRentalDate (only if both provided)
            When(x => x.StartRentalDate.HasValue && x.EndRentalDate.HasValue, () =>
            {
                RuleFor(x => x.EndRentalDate)
                    .GreaterThan(x => x.StartRentalDate)
                    .WithMessage("EndRentalDate must be later than StartRentalDate.");
            });
        }

        private bool IsValidFileType(string fileName)
        {
            string[] permittedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(fileName).ToLower();
            return permittedExtensions.Contains(ext);
        }
    }
}

