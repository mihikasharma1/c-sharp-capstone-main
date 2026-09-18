using System.ComponentModel.DataAnnotations;
using UserService.Validation;
using Xunit;

namespace UserService.Tests.Validation;

public class ValidPhoneNumberAttributeTests
{
    private readonly ValidPhoneNumberAttribute _attribute = new();
    private readonly ValidationContext _context = new(new object());

    [Theory]
    [InlineData("+1-555-0123")]
    [InlineData("5550123")]
    [InlineData("+44 20 7946 0958")]
    public void IsValid_AcceptsValidFormats(string phone)
    {
        Assert.Equal(ValidationResult.Success, _attribute.GetValidationResult(phone, _context));
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("")]
    [InlineData("123")]
    public void IsValid_RejectsInvalidFormats(string phone)
    {
        Assert.NotEqual(ValidationResult.Success, _attribute.GetValidationResult(phone, _context));
    }
}