using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ReservationService.Models;
using ReservationService.Repositories;
using ReservationService.Services;
using Xunit;

namespace ReservationService.Tests.Services;

public class WaitlistCascadeServiceTests
{
    private readonly Mock<IWaitlistRepository> _waitlistRepository = new();
    private readonly Mock<IReservationRepository> _reservationRepository = new();
    private readonly Mock<ICatalogServiceClient> _catalogServiceClient = new();
    private readonly WaitlistCascadeService _service;

    public WaitlistCascadeServiceTests()
    {
        _service = new WaitlistCascadeService(
            _waitlistRepository.Object, _reservationRepository.Object, _catalogServiceClient.Object,
            NullLogger<WaitlistCascadeService>.Instance);
    }

    [Fact]
    public async Task CascadeAsync_ReleasesToGeneralAvailability_WhenNoWaitlistEntries()
    {
        var bookId = Guid.NewGuid();
        _waitlistRepository.Setup(r => r.GetNextWaitingEntryAsync(bookId)).ReturnsAsync((Waitlist?)null);

        await _service.CascadeAsync(bookId);

        _catalogServiceClient.Verify(c => c.UpdateAvailabilityAsync(bookId, 1), Times.Once);
        _reservationRepository.Verify(r => r.AddAsync(It.IsAny<Reservation>()), Times.Never);
    }

    [Fact]
    public async Task CascadeAsync_CreatesReservationForEligibleEntry_WithoutReleasingAvailability()
    {
        var bookId = Guid.NewGuid();
        var entry = new Waitlist { WaitlistId = Guid.NewGuid(), BookId = bookId, UserId = Guid.NewGuid(), Status = WaitlistStatus.Waiting };
        _waitlistRepository.Setup(r => r.GetNextWaitingEntryAsync(bookId)).ReturnsAsync(entry);
        _reservationRepository.Setup(r => r.GetActiveCountAsync(entry.UserId)).ReturnsAsync(2);

        await _service.CascadeAsync(bookId);

        Assert.Equal(WaitlistStatus.Notified, entry.Status);
        Assert.NotNull(entry.ClaimDeadline);
        _reservationRepository.Verify(r => r.AddAsync(It.IsAny<Reservation>()), Times.Once);
        _catalogServiceClient.Verify(c => c.UpdateAvailabilityAsync(It.IsAny<Guid>(), It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task CascadeAsync_SkipsIneligibleEntry_AndTriesNext()
    {
        var bookId = Guid.NewGuid();
        var ineligible = new Waitlist { WaitlistId = Guid.NewGuid(), BookId = bookId, UserId = Guid.NewGuid(), Status = WaitlistStatus.Waiting };
        var eligible = new Waitlist { WaitlistId = Guid.NewGuid(), BookId = bookId, UserId = Guid.NewGuid(), Status = WaitlistStatus.Waiting };

        _waitlistRepository.SetupSequence(r => r.GetNextWaitingEntryAsync(bookId))
            .ReturnsAsync(ineligible)
            .ReturnsAsync(eligible);
        _reservationRepository.Setup(r => r.GetActiveCountAsync(ineligible.UserId)).ReturnsAsync(5);
        _reservationRepository.Setup(r => r.GetActiveCountAsync(eligible.UserId)).ReturnsAsync(0);

        await _service.CascadeAsync(bookId);

        Assert.Equal(WaitlistStatus.Expired, ineligible.Status);
        Assert.Equal(WaitlistStatus.Notified, eligible.Status);
    }
}