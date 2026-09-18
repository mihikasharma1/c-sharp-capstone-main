using System.ComponentModel.DataAnnotations;
using UserService.Validation;

namespace UserService.DTOs;

public class RegisterRequestDto
{
    [Required, EmailAddress, MaxLength(255)]
    public string Email { get; set; } = string.Empty;

    [Required, StrongPassword]
    public string Password { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Required, MaxLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Required, ValidPhoneNumber]
    public string PhoneNumber { get; set; } = string.Empty;
}

public class RegisterResponseDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string MembershipStatus { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class LoginRequestDto
{
    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Password { get; set; } = string.Empty;
}

public class UserSummaryDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}

public class LoginResponseDto
{
    public string AccessToken { get; set; } = string.Empty;
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
    public UserSummaryDto User { get; set; } = new();
}