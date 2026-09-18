using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using UserService.Configuration;
using UserService.Controllers;
using UserService.DTOs;
using UserService.Models;
using UserService.Repositories;
using UserService.Services;
using Xunit;

namespace UserService.Tests.Controllers;

public class AuthControllerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IPasswordHasher> _passwordHasher = new();
    private readonly Mock<IJwtTokenService> _jwtTokenService = new();
    private readonly AuthController _controller;

    public AuthControllerTests()
    {
        _controller = new AuthController(
            _userRepository.Object,
            _passwordHasher.Object,
            _jwtTokenService.Object,
            Options.Create(new JwtOptions { ExpiresInSeconds = 86400 }),
            Mock.Of<ILogger<AuthController>>());
    }

    [Fact]
    public async Task Register_ReturnsCreated_WhenEmailIsNew()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        _passwordHasher.Setup(p => p.Hash(It.IsAny<string>())).Returns("hashed");

        var result = await _controller.Register(new RegisterRequestDto
        {
            Email = "new@example.com", Password = "SecurePass123!",
            FirstName = "New", LastName = "User", PhoneNumber = "+1-555-0100"
        });

        var created = Assert.IsType<ObjectResult>(result);
        Assert.Equal(201, created.StatusCode);
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task Register_ReturnsBadRequest_WhenEmailAlreadyExists()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync(new User { Email = "existing@example.com" });

        var result = await _controller.Register(new RegisterRequestDto
        {
            Email = "existing@example.com", Password = "SecurePass123!",
            FirstName = "A", LastName = "B", PhoneNumber = "+1-555-0100"
        });

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Equal("VALIDATION_ERROR", Assert.IsType<ErrorResponseDto>(badRequest.Value).Error);
    }

    [Fact]
    public async Task Login_ReturnsToken_WhenCredentialsAreValid()
    {
        var user = new User { UserId = Guid.NewGuid(), Email = "test@example.com", PasswordHash = "hashed", Role = Role.Patron };
        _userRepository.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify("correct", "hashed")).Returns(true);
        _jwtTokenService.Setup(j => j.GenerateToken(user)).Returns("fake-jwt-token");

        var result = await _controller.Login(new LoginRequestDto { Email = user.Email, Password = "correct" });

        var body = Assert.IsType<LoginResponseDto>(Assert.IsType<OkObjectResult>(result).Value);
        Assert.Equal("fake-jwt-token", body.AccessToken);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenPasswordIsWrong()
    {
        var user = new User { Email = "test@example.com", PasswordHash = "hashed" };
        _userRepository.Setup(r => r.GetByEmailAsync(user.Email)).ReturnsAsync(user);
        _passwordHasher.Setup(p => p.Verify(It.IsAny<string>(), "hashed")).Returns(false);

        var result = await _controller.Login(new LoginRequestDto { Email = user.Email, Password = "wrong" });

        Assert.IsType<UnauthorizedObjectResult>(result);
    }

    [Fact]
    public async Task Login_ReturnsUnauthorized_WhenUserDoesNotExist()
    {
        _userRepository.Setup(r => r.GetByEmailAsync(It.IsAny<string>())).ReturnsAsync((User?)null);
        var result = await _controller.Login(new LoginRequestDto { Email = "nobody@example.com", Password = "x" });
        Assert.IsType<UnauthorizedObjectResult>(result);
    }
}