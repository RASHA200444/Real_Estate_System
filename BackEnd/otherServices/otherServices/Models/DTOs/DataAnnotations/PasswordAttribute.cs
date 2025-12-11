using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace otherServices.Models.DTOs.DataAnnotations
{
    public class PasswordAttribute : ValidationAttribute
    {
        protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
        {
            string password = value.ToString()!;

            if (string.IsNullOrWhiteSpace(password))
                return new ValidationResult("New password is required.");

            if (password.Length < 8)
                return new ValidationResult("Password must be at least 8 characters long.");

            if (!Regex.IsMatch(password, "[A-Z]"))
                return new ValidationResult("Password must contain at least one uppercase letter.");

            if (!Regex.IsMatch(password, "[a-z]"))
                return new ValidationResult("Password must contain at least one lowercase letter.");

            if (!Regex.IsMatch(password, "[0-9]"))
                return new ValidationResult("Password must contain at least one number.");

            if (!Regex.IsMatch(password, "[^a-zA-Z0-9]"))
                return new ValidationResult("Password must contain at least one special character (!@#$%^&* etc).");

            // validate confirm password (needs access to whole DTO)
            var confirmProp = validationContext.ObjectType.GetProperty("ConfirmPassword");
            if (confirmProp != null)
            {
                var confirmValue = confirmProp.GetValue(validationContext.ObjectInstance)?.ToString();
                if (password != confirmValue)
                    return new ValidationResult("Password must equal ConfirmPassword.");
            }

            return ValidationResult.Success;
        }
    }
}