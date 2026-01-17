using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;
using otherServices.Models.Enums;
using FluentValidation;

namespace otherServices.Models.DTOs
{
    public class SubmitProposalDto
    {
        public string Phone { get; set; }
        public DateTime? StartRentalDate { get; set; }

        public DateTime? EndRentalDate { get; set; }
        public IsInstallment IsInstallment { get; set; } // Cash , Installment
        public double Offeredprice { get; set; }
        public IFormFile File { get; set; }

    }



    public class SubmitProposalDtoValidator : AbstractValidator<SubmitProposalDto>
        {
            public SubmitProposalDtoValidator()
            {
                // Phone validation
                RuleFor(x => x.Phone)
                    .NotEmpty().WithMessage("Phone number is required.")
                    .Matches(@"^(01[0125][0-9]{8})$").WithMessage("Invalid Egyptian phone number.");

                // Offered price
                RuleFor(x => x.Offeredprice)
                    .GreaterThan(0).WithMessage("Offered price must be greater than zero.");


            // File validation
            RuleFor(x => x.File)
                .NotNull().WithMessage("File is required.")
                .Must(f => f != null && f.Length > 0).WithMessage("File cannot be empty.")
                .Must(f => f == null || IsValidFileType(f.FileName)).WithMessage("Only PDF, JPG, PNG files are allowed.")
                .Must(f => f == null || f.Length <= 5 * 1024 * 1024).WithMessage("File size must be less than 5MB.");

            // StartRentalDate must be less than EndRentalDate (Basic DTO check)
            RuleFor(x => x.EndRentalDate)
                    .GreaterThan(x => x.StartRentalDate)
                    .When(x => x.StartRentalDate != default && x.EndRentalDate != default)
                    .WithMessage("EndRentalDate must be later than StartRentalDate.");
            }

            private bool IsValidFileType(string fileName)
            {
                string[] permittedExtensions = { ".pdf", ".jpg", ".jpeg", ".png" };
                var ext = Path.GetExtension(fileName).ToLower();
                return permittedExtensions.Contains(ext);
            }
        }

}
