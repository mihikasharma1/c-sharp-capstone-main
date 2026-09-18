using UserService.Services;
using Xunit;

namespace UserService.Tests.Services;

public class BCryptPasswordHasherTests
{
    private readonly BCryptPasswordHasher _hasher = new();

    [Fact]
    public void Hash_ProducesDifferentValueThanPlainText()
    {
        Assert.NotEqual("MyPassword123!", _hasher.Hash("MyPassword123!"));
    }

    [Fact]
    public void Verify_ReturnsTrue_ForCorrectPassword()
    {
        var hash = _hasher.Hash("MyPassword123!");
        Assert.True(_hasher.Verify("MyPassword123!", hash));
    }

    [Fact]
    public void Verify_ReturnsFalse_ForIncorrectPassword()
    {
        var hash = _hasher.Hash("MyPassword123!");
        Assert.False(_hasher.Verify("WrongPassword!", hash));
    }
}