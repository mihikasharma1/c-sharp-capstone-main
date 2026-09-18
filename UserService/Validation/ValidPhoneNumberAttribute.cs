using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace UserService.Validation;

public partial class ValidPhoneNumberAttribute : ValidationAttribute
{
    [GeneratedRegex(@"^\+?[0-9][0-9\-\s]{6,19}$")]
    private static partial Regex PhoneRegex();

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        if (value is not string phone || string.IsNullOrWhiteSpace(phone))
            return new ValidationResult("Phone number is required.");

        return PhoneRegex().IsMatch(phone)
            ? ValidationResult.Success
            : new ValidationResult("Phone number format is invalid.");
    }
}