using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace otherServices.Models.DTOs.DataAnnotations
{
    public class UsernameAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            if (value == null)
                return ValidationResult.Success;

            string username = value.ToString()!;

            //if (string.IsNullOrWhiteSpace(username))
            //    return new ValidationResult("Username is required.");

            if (username.Length < 3)
                return new ValidationResult("Username must be at least 3 characters.");

            if (username.Length > 20)
                return new ValidationResult("Username must not exceed 20 characters.");

            // regex: letters, numbers, single hyphens, no start/end hyphens
            var regex = new Regex(@"^[a-zA-Z0-9]+(-[a-zA-Z0-9]+)*$");

            if (!regex.IsMatch(username))
                return new ValidationResult("Username may only contain letters, numbers, or single hyphens, and cannot begin or end with a hyphen.");

            return ValidationResult.Success;
        }
    }
}
