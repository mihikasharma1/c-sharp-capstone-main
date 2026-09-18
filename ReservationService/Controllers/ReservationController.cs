using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ReservationService.DTOs;
using ReservationService.Exceptions;
using ReservationService.Services;

namespace ReservationService.Controllers;

[ApiController]
[Route("api/reservations")]
public class ReservationsController : ControllerBase
{
    private readonly IReservationWorkflowService _reservationWorkflowService;
    private readonly IWaitlistWorkflowService _waitlistWorkflowService;

    public ReservationsController(
        IReservationWorkflowService reservationWorkflowService,
        IWaitlistWorkflowService waitlistWorkflowService)
    {
        _reservationWorkflowService = reservationWorkflowService;
        _waitlistWorkflowService = waitlistWorkflowService;
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst("userId")?.Value;
        if (claim is null || !Guid.TryParse(claim, out var userId))
            throw new UnauthorizedApiException("Authentication required");
        return userId;
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateReservation(ReservationCreateRequestDto request)
    {
        var result = await _reservationWorkflowService.CreateReservationAsync(GetUserId(), request.BookId);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetActiveReservations()
    {
        var result = await _reservationWorkflowService.GetActiveReservationsAsync(GetUserId());
        return Ok(result);
    }

    [HttpPost("{reservationId:guid}/checkout")]
    [Authorize(Roles = "LIBRARIAN")]
    public async Task<IActionResult> Checkout(Guid reservationId, CheckoutRequestDto request)
    {
        var result = await _reservationWorkflowService.CheckoutAsync(reservationId, request.Notes);
        return Ok(result);
    }

    [HttpPost("{reservationId:guid}/return")]
    [Authorize(Roles = "LIBRARIAN")]
    public async Task<IActionResult> Return(Guid reservationId, ReturnRequestDto request)
    {
        var result = await _reservationWorkflowService.ReturnAsync(reservationId, request.Condition, request.Notes);
        return Ok(result);
    }

    [HttpGet("history")]
    [Authorize]
    public async Task<IActionResult> GetHistory([FromQuery] int page = 0, [FromQuery] int size = 20)
    {
        var result = await _reservationWorkflowService.GetHistoryAsync(GetUserId(), page, size);
        return Ok(result);
    }

    [HttpGet("statistics/{userId:guid}")]
    public async Task<IActionResult> GetStatistics(Guid userId)
    {
        var result = await _reservationWorkflowService.GetStatisticsAsync(userId);
        return Ok(result);
    }

    [HttpPost("waitlist")]
    [Authorize]
    public async Task<IActionResult> JoinWaitlist(WaitlistJoinRequestDto request)
    {
        var result = await _waitlistWorkflowService.JoinWaitlistAsync(GetUserId(), request.BookId);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpGet("waitlist")]
    [Authorize]
    public async Task<IActionResult> GetMyWaitlist()
    {
        var result = await _waitlistWorkflowService.GetMyWaitlistAsync(GetUserId());
        return Ok(result);
    }

    [HttpDelete("waitlist/{waitlistId:guid}")]
    [Authorize]
    public async Task<IActionResult> CancelWaitlist(Guid waitlistId)
    {
        var result = await _waitlistWorkflowService.CancelWaitlistAsync(GetUserId(), waitlistId);
        return Ok(result);
    }
}