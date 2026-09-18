using System.ComponentModel.DataAnnotations;
using UserService.Validation;
using Xunit;

namespace UserService.Tests.Validation;

public class StrongPasswordAttributeTests
{
    private readonly StrongPasswordAttribute _attribute = new();
    private readonly ValidationContext _context = new(new object());

    [Theory]
    [InlineData("Short1!")]
    [InlineData("alllowercase1!")]
    [InlineData("ALLUPPERCASE1!")]
    [InlineData("NoDigitsHere!")]
    [InlineData("NoSpecialChar1")]
    public void IsValid_RejectsWeakPasswords(string password)
    {
        var result = _attribute.GetValidationResult(password, _context);
        Assert.NotEqual(ValidationResult.Success, result);
    }

    [Fact]
    public void IsValid_AcceptsStrongPassword()
    {
        var result = _attribute.GetValidationResult("SecurePass123!", _context);
        Assert.Equal(ValidationResult.Success, result);
    }

    [Fact]
    public void IsValid_RejectsNull()
    {
        var result = _attribute.GetValidationResult(null, _context);
        Assert.NotEqual(ValidationResult.Success, result);
    }
}