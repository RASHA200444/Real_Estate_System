using FluentValidation;
using otherServices.Models.Enums;
using System.ComponentModel.DataAnnotations;
using FluentValidation;

namespace otherServices.Models.DTOs
{
    public class CreatePostDTO
    {
        [Required]
        [StringLength(255)]
        public string Title { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public double Price { get; set; }

        [Required]
        [StringLength(255)]
        public string Location { get; set; }

        [Required]
        public string LocationPath { get; set; }

        // Apartment specifications
        public int NumOfRooms { get; set; }
        public int NumOfBathrooms { get; set; }
        public double Area { get; set; } // in square meters
        public int? TotalUnitsInBuilding { get; set; } // nullable

        // Additional optional attributes
        public bool IsFurnished { get; set; }
        public bool HasGarage { get; set; }
        public int? FloorNumber { get; set; }
        public PropertyType Type { get; set; } // Rent / Sale

        public DateTime? StartRentalDate { get; set; }
        public DateTime? EndRentalDate { get; set; }


        public List<string>? Tags { get; set; }   // user enters tags in UI -> sent as array


        [Required(ErrorMessage = "Post document is required")]
        public IFormFile PostDocFile { get; set; }  

        public List<IFormFile>? Images { get; set; }  




    }

    public class CreatePostDTOValidator : AbstractValidator<CreatePostDTO>
        {
            public CreatePostDTOValidator()
            {
                RuleFor(x => x.Price)
                    .GreaterThan(0).WithMessage("Price must be greater than zero.");

                RuleFor(x => x.NumOfRooms)
                    .GreaterThan(0).WithMessage("Number of rooms must be greater than zero.");

                RuleFor(x => x.NumOfBathrooms)
                    .GreaterThan(0).WithMessage("Number of bathrooms must be greater than zero.");

                RuleFor(x => x.Area)
                    .GreaterThan(0).WithMessage("Area must be greater than zero.");

                RuleFor(x => x.TotalUnitsInBuilding)
                    .GreaterThan(0).When(x => x.TotalUnitsInBuilding.HasValue)
                    .WithMessage("Total units in building must be greater than zero.");

                RuleFor(x => x.FloorNumber)
                    .GreaterThan(0).When(x => x.FloorNumber.HasValue)
                    .WithMessage("Floor number must be greater than zero.");
                RuleFor(x => x.StartRentalDate)
                    .Null()
                    .When(x => x.Type == PropertyType.Sale)
                    .WithMessage("StartRentalDate is not allowed when the property type is Sale.");

                RuleFor(x => x.EndRentalDate)
                    .Null()
                    .When(x => x.Type == PropertyType.Sale)
                    .WithMessage("EndRentalDate is not allowed when the property type is Sale.");

        }
    }

}
