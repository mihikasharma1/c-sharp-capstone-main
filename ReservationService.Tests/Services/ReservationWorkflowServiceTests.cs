using Moq;
using ReservationService.Exceptions;
using ReservationService.Models;
using ReservationService.Repositories;
using ReservationService.Services;
using Xunit;

namespace ReservationService.Tests.Services;

public class ReservationWorkflowServiceTests
{
    private readonly Mock<IReservationRepository> _reservationRepository = new();
    private readonly Mock<ICatalogServiceClient> _catalogServiceClient = new();
    private readonly Mock<IUserServiceClient> _userServiceClient = new();
    private readonly Mock<IWaitlistCascadeService> _waitlistCascadeService = new();
    private readonly ReservationWorkflowService _service;

    public ReservationWorkflowServiceTests()
    {
        _service = new ReservationWorkflowService(
            _reservationRepository.Object, _catalogServiceClient.Object, _userServiceClient.Object, _waitlistCascadeService.Object);
    }

    private void SetupValidActiveUser(Guid userId) =>
        _userServiceClient.Setup(c => c.ValidateUserAsync(userId)).ReturnsAsync(new UserValidationDto { UserId = userId, MembershipStatus = "ACTIVE" });

    [Fact]
    public async Task CreateReservationAsync_Succeeds_WhenUserAndBookAreEligible()
    {
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        SetupValidActiveUser(userId);
        _reservationRepository.Setup(r => r.GetActiveCountAsync(userId)).ReturnsAsync(2);
        _catalogServiceClient.Setup(c => c.GetBookAsync(bookId)).ReturnsAsync(new CatalogBookDto { BookId = bookId, Title = "Clean Code", Author = "Robert Martin", AvailableCopies = 3 });
        _catalogServiceClient.Setup(c => c.UpdateAvailabilityAsync(bookId, -1)).ReturnsAsync(true);

        var result = await _service.CreateReservationAsync(userId, bookId);

        Assert.Equal("RESERVED", result.Status);
        _reservationRepository.Verify(r => r.AddAsync(It.IsAny<Reservation>()), Times.Once);
    }

    [Fact]
    public async Task CreateReservationAsync_ThrowsLimitExceeded_WhenUserHasFiveActiveReservations()
    {
        var userId = Guid.NewGuid();
        SetupValidActiveUser(userId);
        _reservationRepository.Setup(r => r.GetActiveCountAsync(userId)).ReturnsAsync(5);

        var ex = await Assert.ThrowsAsync<ReservationLimitExceededException>(() => _service.CreateReservationAsync(userId, Guid.NewGuid()));
        Assert.Equal(5, ex.CurrentReservations);
    }

    [Fact]
    public async Task CreateReservationAsync_ThrowsBookUnavailable_WhenNoCopiesLeft()
    {
        var userId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        SetupValidActiveUser(userId);
        _reservationRepository.Setup(r => r.GetActiveCountAsync(userId)).ReturnsAsync(0);
        _catalogServiceClient.Setup(c => c.GetBookAsync(bookId)).ReturnsAsync(new CatalogBookDto { BookId = bookId, AvailableCopies = 0 });

        var ex = await Assert.ThrowsAsync<BookUnavailableException>(() => _service.CreateReservationAsync(userId, bookId));
        Assert.Equal(0, ex.AvailableCopies);
    }

    [Fact]
    public async Task CreateReservationAsync_ThrowsForbidden_WhenUserIsSuspended()
    {
        var userId = Guid.NewGuid();
        _userServiceClient.Setup(c => c.ValidateUserAsync(userId)).ReturnsAsync(new UserValidationDto { UserId = userId, MembershipStatus = "SUSPENDED" });
        await Assert.ThrowsAsync<ForbiddenException>(() => _service.CreateReservationAsync(userId, Guid.NewGuid()));
    }

    [Fact]
    public async Task CreateReservationAsync_ThrowsNotFound_WhenBookDoesNotExist()
    {
        var userId = Guid.NewGuid();
        SetupValidActiveUser(userId);
        _reservationRepository.Setup(r => r.GetActiveCountAsync(userId)).ReturnsAsync(0);
        _catalogServiceClient.Setup(c => c.GetBookAsync(It.IsAny<Guid>())).ReturnsAsync((CatalogBookDto?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CreateReservationAsync(userId, Guid.NewGuid()));
    }

    [Fact]
    public async Task CheckoutAsync_UpdatesStatusAndSetsDueDate()
    {
        var reservation = new Reservation { ReservationId = Guid.NewGuid(), Status = ReservationStatus.Reserved };
        _reservationRepository.Setup(r => r.GetByIdAsync(reservation.ReservationId)).ReturnsAsync(reservation);

        var result = await _service.CheckoutAsync(reservation.ReservationId, "Good condition");

        Assert.Equal("CHECKED_OUT", result.Status);
        Assert.Equal(reservation.CheckedOutAt!.Value.AddDays(14), result.DueDate);
    }

    [Fact]
    public async Task CheckoutAsync_ThrowsInvalidStatus_WhenNotReserved()
    {
        var reservation = new Reservation { ReservationId = Guid.NewGuid(), Status = ReservationStatus.CheckedOut };
        _reservationRepository.Setup(r => r.GetByIdAsync(reservation.ReservationId)).ReturnsAsync(reservation);
        var ex = await Assert.ThrowsAsync<InvalidStatusException>(() => _service.CheckoutAsync(reservation.ReservationId, null));
        Assert.Equal("CHECKED_OUT", ex.CurrentStatus);
    }

    [Fact]
    public async Task CheckoutAsync_ThrowsNotFound_WhenReservationDoesNotExist()
    {
        _reservationRepository.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Reservation?)null);
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CheckoutAsync(Guid.NewGuid(), null));
    }

    [Fact]
    public async Task ReturnAsync_CalculatesNoLateFee_WhenReturnedOnTime()
    {
        var reservation = new Reservation { ReservationId = Guid.NewGuid(), BookId = Guid.NewGuid(), Status = ReservationStatus.CheckedOut, DueDate = DateTime.UtcNow.AddDays(3) };
        _reservationRepository.Setup(r => r.GetByIdAsync(reservation.ReservationId)).ReturnsAsync(reservation);

        var result = await _service.ReturnAsync(reservation.ReservationId, "GOOD", null);

        Assert.Equal(0, result.LateDays);
        Assert.Equal(0m, result.LateFee);
        _waitlistCascadeService.Verify(c => c.CascadeAsync(reservation.BookId), Times.Once);
    }

    [Fact]
    public async Task ReturnAsync_CalculatesLateFee_WhenOverdue()
    {
        var reservation = new Reservation
        {
            ReservationId = Guid.NewGuid(),
            BookId = Guid.NewGuid(),
            Status = ReservationStatus.CheckedOut,
            DueDate = DateTime.UtcNow.AddHours(-36) // 1.5 days overdue -> Ceiling rounds up to 2, with safety margin
        };
        _reservationRepository.Setup(r => r.GetByIdAsync(reservation.ReservationId)).ReturnsAsync(reservation);

        var result = await _service.ReturnAsync(reservation.ReservationId, "FAIR", null);

        Assert.Equal(2, result.LateDays);
        Assert.Equal(2.00m, result.LateFee);
    }

    [Fact]
    public async Task ReturnAsync_ThrowsInvalidStatus_WhenNotCheckedOut()
    {
        var reservation = new Reservation { ReservationId = Guid.NewGuid(), Status = ReservationStatus.Reserved };
        _reservationRepository.Setup(r => r.GetByIdAsync(reservation.ReservationId)).ReturnsAsync(reservation);
        await Assert.ThrowsAsync<InvalidStatusException>(() => _service.ReturnAsync(reservation.ReservationId, "GOOD", null));
    }

    [Fact]
    public async Task GetStatisticsAsync_ReturnsCountsFromRepository()
    {
        var userId = Guid.NewGuid();
        _reservationRepository.Setup(r => r.GetActiveCountAsync(userId)).ReturnsAsync(3);
        _reservationRepository.Setup(r => r.GetCompletedCountAsync(userId)).ReturnsAsync(10);

        var result = await _service.GetStatisticsAsync(userId);

        Assert.Equal(3, result.ActiveReservations);
        Assert.Equal(10, result.BorrowingHistory);
    }
}