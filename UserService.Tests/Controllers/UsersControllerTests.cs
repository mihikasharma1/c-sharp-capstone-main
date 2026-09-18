using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using UserService.Controllers;
using UserService.DTOs;
using UserService.Models;
using UserService.Repositories;
using UserService.Services;
using Xunit;

namespace UserService.Tests.Controllers;

public class UsersControllerTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IReservationServiceClient> _reservationServiceClient = new();
    private readonly UsersController _controller;

    public UsersControllerTests()
    {
        _controller = new UsersController(_userRepository.Object, _reservationServiceClient.Object);
    }

    private void SetUser(Guid userId)
    {
        var identity = new ClaimsIdentity(new[] { new Claim("userId", userId.ToString()) }, "TestAuth");
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) }
        };
    }

    [Fact]
    public async Task GetProfile_ReturnsProfileWithStatistics()
    {
        var userId = Guid.NewGuid();
        SetUser(userId);
        _userRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User
        {
            UserId = userId, Role = Role.Patron, MembershipStatus = MembershipStatus.Active
        });
        _reservationServiceClient.Setup(c => c.GetStatisticsAsync(userId))
            .ReturnsAsync(new ReservationStatisticsDto { ActiveReservations = 2, BorrowingHistory = 5 });

        var dto = Assert.IsType<ProfileResponseDto>(Assert.IsType<OkObjectResult>(await _controller.GetProfile()).Value);

        Assert.Equal(2, dto.ActiveReservations);
        Assert.Equal(5, dto.BorrowingHistory);
    }

    [Fact]
    public async Task GetProfile_DefaultsStatisticsToZero_WhenReservationServiceUnavailable()
    {
        var userId = Guid.NewGuid();
        SetUser(userId);
        _userRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User { UserId = userId, Role = Role.Patron });
        _reservationServiceClient.Setup(c => c.GetStatisticsAsync(userId)).ReturnsAsync((ReservationStatisticsDto?)null);

        var dto = Assert.IsType<ProfileResponseDto>(Assert.IsType<OkObjectResult>(await _controller.GetProfile()).Value);

        Assert.Equal(0, dto.ActiveReservations);
        Assert.Equal(0, dto.BorrowingHistory);
    }

    [Fact]
    public async Task ValidateUser_ReturnsNotFound_WhenUserDoesNotExist()
    {
        _userRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((User?)null);
        Assert.IsType<NotFoundObjectResult>(await _controller.ValidateUser(Guid.NewGuid()));
    }

    [Fact]
    public async Task ValidateUser_ReturnsBadRequest_WhenUserSuspended()
    {
        var userId = Guid.NewGuid();
        _userRepository.Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync(new User { UserId = userId, MembershipStatus = MembershipStatus.Suspended });

        Assert.IsType<BadRequestObjectResult>(await _controller.ValidateUser(userId));
    }
    
    [Fact]
    public async Task ValidateUser_ReturnsOk_WhenUserIsActive()
    {
        var userId = Guid.NewGuid();
        _userRepository.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(new User
        {
            UserId = userId, Role = Role.Patron, MembershipStatus = MembershipStatus.Active
        });
        _reservationServiceClient.Setup(c => c.GetStatisticsAsync(userId))
            .ReturnsAsync(new ReservationStatisticsDto { ActiveReservations = 3 });

        var dto = Assert.IsType<UserValidateResponseDto>(Assert.IsType<OkObjectResult>(await _controller.ValidateUser(userId)).Value);
        Assert.Equal(3, dto.ActiveReservationsCount);
    }
}