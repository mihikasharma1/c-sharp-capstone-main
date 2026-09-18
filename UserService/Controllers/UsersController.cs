using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using UserService.DTOs;
using UserService.Models;
using UserService.Repositories;
using UserService.Services;

namespace UserService.Controllers;

[ApiController]
[Route("api")]
public class UsersController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IReservationServiceClient _reservationServiceClient;

    public UsersController(IUserRepository userRepository, IReservationServiceClient reservationServiceClient)
    {
        _userRepository = userRepository;
        _reservationServiceClient = reservationServiceClient;
    }

    [HttpGet("users/profile")]
    [Authorize]
    public async Task<IActionResult> GetProfile()
    {
        var userIdClaim = User.FindFirst("userId")?.Value;
        if (userIdClaim is null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized(new ErrorResponseDto { Error = "UNAUTHORIZED", Message = "Authentication required" });
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
        {
            return Unauthorized(new ErrorResponseDto { Error = "UNAUTHORIZED", Message = "Authentication required" });
        }

        var stats = await _reservationServiceClient.GetStatisticsAsync(userId);

        return Ok(new ProfileResponseDto
        {
            UserId = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            PhoneNumber = user.PhoneNumber,
            Role = user.Role.ToString().ToUpperInvariant(),
            MembershipStatus = user.MembershipStatus.ToString().ToUpperInvariant(),
            MemberSince = user.MemberSince,
            ActiveReservations = stats?.ActiveReservations ?? 0,
            BorrowingHistory = stats?.BorrowingHistory ?? 0
        });
    }

    [HttpGet("users/{userId:guid}/validate")]
    public async Task<IActionResult> ValidateUser(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user is null)
        {
            return NotFound(new ErrorResponseDto { Error = "NOT_FOUND", Message = $"User not found with ID: {userId}" });
        }

        if (user.MembershipStatus != MembershipStatus.Active)
        {
            return BadRequest(new ErrorResponseDto { Error = "USER_SUSPENDED", Message = "User account is suspended" });
        }

        var stats = await _reservationServiceClient.GetStatisticsAsync(userId);

        return Ok(new UserValidateResponseDto
        {
            UserId = user.UserId,
            Email = user.Email,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Role = user.Role.ToString().ToUpperInvariant(),
            MembershipStatus = user.MembershipStatus.ToString().ToUpperInvariant(),
            ActiveReservationsCount = stats?.ActiveReservations ?? 0
        });
    }
}