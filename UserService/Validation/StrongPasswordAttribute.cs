using System.ComponentModel.DataAnnotations;

namespace UserService.Validation;

public class StrongPasswordAttribute : ValidationAttribute
{
    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string password || string.IsNullOrEmpty(password))
            return new ValidationResult("Password is required.");

        if (password.Length < 8)
            return new ValidationResult("Password must be at least 8 characters long.");
        if (!password.Any(char.IsUpper))
            return new ValidationResult("Password must contain at least one uppercase letter.");
        if (!password.Any(char.IsLower))
            return new ValidationResult("Password must contain at least one lowercase letter.");
        if (!password.Any(char.IsDigit))
            return new ValidationResult("Password must contain at least one digit.");
        if (!password.Any(c => !char.IsLetterOrDigit(c)))
            return new ValidationResult("Password must contain at least one special character.");

        return ValidationResult.Success;
    }
}