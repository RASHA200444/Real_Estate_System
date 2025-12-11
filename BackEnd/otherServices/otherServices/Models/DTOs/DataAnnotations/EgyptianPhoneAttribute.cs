using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace otherServices.Models.DTOs.DataAnnotations
{
    public class EgyptianPhoneAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            string phone = value.ToString()!;

            if (string.IsNullOrWhiteSpace(phone))
                return ValidationResult.Success;

            var regex = new Regex(@"^(01[0125][0-9]{8})$");

            if (!regex.IsMatch(phone))
                return new ValidationResult("Invalid Egyptian phone number.");

            return ValidationResult.Success;
        }
    }
}