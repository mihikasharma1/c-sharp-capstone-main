using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using ReservationService.Controllers;
using ReservationService.DTOs;
using ReservationService.Services;
using Xunit;

namespace ReservationService.Tests.Controllers;

public class ReservationsControllerTests
{
    private readonly Mock<IReservationWorkflowService> _reservationWorkflowService = new();
    private readonly Mock<IWaitlistWorkflowService> _waitlistWorkflowService = new();
    private readonly ReservationsController _controller;

    public ReservationsControllerTests()
    {
        _controller = new ReservationsController(_reservationWorkflowService.Object, _waitlistWorkflowService.Object);
    }

    private void SetUser(Guid userId)
    {
        var identity = new ClaimsIdentity(new[] { new Claim("userId", userId.ToString()) }, "TestAuth");
        _controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) } };
    }

    [Fact]
    public async Task CreateReservation_ReturnsCreatedResult()
    {
        var userId = Guid.NewGuid();
        SetUser(userId);
        var response = new ReservationResponseDto { ReservationId = Guid.NewGuid() };
        _reservationWorkflowService.Setup(s => s.CreateReservationAsync(userId, It.IsAny<Guid>())).ReturnsAsync(response);

        var created = Assert.IsType<ObjectResult>(await _controller.CreateReservation(new ReservationCreateRequestDto { BookId = Guid.NewGuid() }));

        Assert.Equal(201, created.StatusCode);
        Assert.Same(response, created.Value);
    }

    [Fact]
    public async Task GetHistory_UsesDefaultPagingParameters()
    {
        var userId = Guid.NewGuid();
        SetUser(userId);
        _reservationWorkflowService.Setup(s => s.GetHistoryAsync(userId, 0, 20)).ReturnsAsync(new PagedResultDto<HistoryRecordDto>());

        Assert.IsType<OkObjectResult>(await _controller.GetHistory());

        _reservationWorkflowService.Verify(s => s.GetHistoryAsync(userId, 0, 20), Times.Once);
    }
    
    [Fact]
public async Task GetActiveReservations_ReturnsOk()
{
    var userId = Guid.NewGuid();
    SetUser(userId);
    _reservationWorkflowService.Setup(s => s.GetActiveReservationsAsync(userId))
        .ReturnsAsync(new ActiveReservationsResponseDto { TotalActive = 2 });

    var ok = Assert.IsType<OkObjectResult>(await _controller.GetActiveReservations());
    Assert.Equal(2, Assert.IsType<ActiveReservationsResponseDto>(ok.Value).TotalActive);
}

[Fact]
public async Task Checkout_ReturnsOk()
{
    var response = new CheckoutResponseDto { Status = "CHECKED_OUT" };
    _reservationWorkflowService.Setup(s => s.CheckoutAsync(It.IsAny<Guid>(), It.IsAny<string>()))
        .ReturnsAsync(response);

    var ok = Assert.IsType<OkObjectResult>(await _controller.Checkout(Guid.NewGuid(), new CheckoutRequestDto()));
    Assert.Same(response, ok.Value);
}

[Fact]
public async Task Return_ReturnsOk()
{
    var response = new ReturnResponseDto { LateFee = 0m };
    _reservationWorkflowService.Setup(s => s.ReturnAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>()))
        .ReturnsAsync(response);

    var ok = Assert.IsType<OkObjectResult>(await _controller.Return(Guid.NewGuid(), new ReturnRequestDto { Condition = "GOOD" }));
    Assert.Same(response, ok.Value);
}

[Fact]
public async Task GetStatistics_ReturnsOk()
{
    var userId = Guid.NewGuid();
    _reservationWorkflowService.Setup(s => s.GetStatisticsAsync(userId)).ReturnsAsync(new StatisticsDto { UserId = userId });

    Assert.IsType<OkObjectResult>(await _controller.GetStatistics(userId));
}

[Fact]
public async Task JoinWaitlist_ReturnsCreated()
{
    var userId = Guid.NewGuid();
    SetUser(userId);
    var response = new WaitlistResponseDto { Position = 1 };
    _waitlistWorkflowService.Setup(s => s.JoinWaitlistAsync(userId, It.IsAny<Guid>())).ReturnsAsync(response);

    var created = Assert.IsType<ObjectResult>(await _controller.JoinWaitlist(new WaitlistJoinRequestDto { BookId = Guid.NewGuid() }));
    Assert.Equal(201, created.StatusCode);
}

[Fact]
public async Task GetMyWaitlist_ReturnsOk()
{
    var userId = Guid.NewGuid();
    SetUser(userId);
    _waitlistWorkflowService.Setup(s => s.GetMyWaitlistAsync(userId)).ReturnsAsync(new WaitlistEntriesResponseDto());

    Assert.IsType<OkObjectResult>(await _controller.GetMyWaitlist());
}

[Fact]
public async Task CancelWaitlist_ReturnsOk()
{
    var userId = Guid.NewGuid();
    SetUser(userId);
    var response = new WaitlistCancelResponseDto { Status = "CANCELLED" };
    _waitlistWorkflowService.Setup(s => s.CancelWaitlistAsync(userId, It.IsAny<Guid>())).ReturnsAsync(response);

    var ok = Assert.IsType<OkObjectResult>(await _controller.CancelWaitlist(Guid.NewGuid()));
    Assert.Same(response, ok.Value);
}
}