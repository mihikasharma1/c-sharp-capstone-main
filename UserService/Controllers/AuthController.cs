using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using UserService.Configuration;
using UserService.DTOs;
using UserService.Models;
using UserService.Repositories;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly JwtOptions _jwtOptions;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtTokenService jwtTokenService,
        IOptions<JwtOptions> jwtOptions,
        ILogger<AuthController> logger)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _jwtOptions = jwtOptions.Value;
        _logger = logger;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            var firstError = ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage)
                .FirstOrDefault() ?? "Validation failed.";

            return BadRequest(new ErrorResponseDto { Error = "VALIDATION_ERROR", Message = firstError });
        }

        var existing = await _userRepository.GetByEmailAsync(request.Email);
        if (existing is not null)
        {
            return BadRequest(new ErrorResponseDto { Error = "VALIDATION_ERROR", Message = "Email already exists" });
        }

        var user = new User
        {
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            PhoneNumber = request.PhoneNumber,
            Role = Role.Patron,
            MembershipStatus = MembershipStatus.Active,
            MemberSince = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        return StatusCode(StatusCodes.Status201Created, new RegisterResponseDto
        {
            UserId = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role.ToString().ToUpperInvariant(),
            MembershipStatus = user.MembershipStatus.ToString().ToUpperInvariant(),
            CreatedAt = user.CreatedAt,
            Message = "Registration successful"
        });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequestDto request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Failed login attempt for email {Email}", request.Email);
            return Unauthorized(new ErrorResponseDto { Error = "AUTHENTICATION_FAILED", Message = "Invalid email or password" });
        }

        var token = _jwtTokenService.GenerateToken(user);

        return Ok(new LoginResponseDto
        {
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresIn = _jwtOptions.ExpiresInSeconds,
            User = new UserSummaryDto
            {
                UserId = user.UserId,
                Email = user.Email,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Role = user.Role.ToString().ToUpperInvariant()
            }
        });
    }
}