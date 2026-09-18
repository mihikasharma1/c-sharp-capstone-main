using Moq;
using ReservationService.Exceptions;
using ReservationService.Models;
using ReservationService.Repositories;
using ReservationService.Services;
using Xunit;

namespace ReservationService.Tests.Services;

public class WaitlistWorkflowServiceTests
{
    private readonly Mock<IWaitlistRepository> _waitlistRepository = new();
    private readonly Mock<ICatalogServiceClient> _catalogServiceClient = new();
    private readonly Mock<IWaitlistCascadeService> _waitlistCascadeService = new();
    private readonly WaitlistWorkflowService _service;

    public WaitlistWorkflowServiceTests()
    {
        _service = new WaitlistWorkflowService(_waitlistRepository.Object, _catalogServiceClient.Object, _waitlistCascadeService.Object);
    }

    [Fact]
    public async Task JoinWaitlistAsync_ThrowsBookAvailable_WhenCopiesExist()
    {
        var bookId = Guid.NewGuid();
        _catalogServiceClient.Setup(c => c.GetBookAsync(bookId)).ReturnsAsync(new CatalogBookDto { BookId = bookId, AvailableCopies = 2 });
        await Assert.ThrowsAsync<BookAvailableException>(() => _service.JoinWaitlistAsync(Guid.NewGuid(), bookId));
    }

    [Fact]
    public async Task JoinWaitlistAsync_ThrowsAlreadyWaitlisted_WhenEntryExists()
    {
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _catalogServiceClient.Setup(c => c.GetBookAsync(bookId)).ReturnsAsync(new CatalogBookDto { BookId = bookId, AvailableCopies = 0 });
        _waitlistRepository.Setup(r => r.GetActiveEntryForUserAndBookAsync(userId, bookId)).ReturnsAsync(new Waitlist());
        await Assert.ThrowsAsync<AlreadyWaitlistedException>(() => _service.JoinWaitlistAsync(userId, bookId));
    }

    [Fact]
    public async Task JoinWaitlistAsync_CreatesEntry_WithComputedPosition()
    {
        var bookId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _catalogServiceClient.Setup(c => c.GetBookAsync(bookId)).ReturnsAsync(new CatalogBookDto { BookId = bookId, Title = "Book", Author = "Author", AvailableCopies = 0 });
        _waitlistRepository.Setup(r => r.GetActiveEntryForUserAndBookAsync(userId, bookId)).ReturnsAsync((Waitlist?)null);
        _waitlistRepository.Setup(r => r.GetWaitingPositionAsync(bookId, It.IsAny<DateTime>())).ReturnsAsync(3);

        var result = await _service.JoinWaitlistAsync(userId, bookId);

        Assert.Equal(3, result.Position);
        Assert.Equal("WAITING", result.Status);
    }

    [Fact]
    public async Task CancelWaitlistAsync_ThrowsNotFound_WhenEntryBelongsToDifferentUser()
    {
        var entry = new Waitlist { WaitlistId = Guid.NewGuid(), UserId = Guid.NewGuid() };
        _waitlistRepository.Setup(r => r.GetByIdAsync(entry.WaitlistId)).ReturnsAsync(entry);
        await Assert.ThrowsAsync<NotFoundException>(() => _service.CancelWaitlistAsync(Guid.NewGuid(), entry.WaitlistId));
    }

    [Fact]
    public async Task CancelWaitlistAsync_TriggersCascade_WhenEntryWasNotified()
    {
        var userId = Guid.NewGuid();
        var entry = new Waitlist { WaitlistId = Guid.NewGuid(), UserId = userId, BookId = Guid.NewGuid(), Status = WaitlistStatus.Notified };
        _waitlistRepository.Setup(r => r.GetByIdAsync(entry.WaitlistId)).ReturnsAsync(entry);

        await _service.CancelWaitlistAsync(userId, entry.WaitlistId);

        Assert.Equal(WaitlistStatus.Cancelled, entry.Status);
        _waitlistCascadeService.Verify(c => c.CascadeAsync(entry.BookId), Times.Once);
    }

    [Fact]
    public async Task CancelWaitlistAsync_DoesNotTriggerCascade_WhenEntryWasOnlyWaiting()
    {
        var userId = Guid.NewGuid();
        var entry = new Waitlist { WaitlistId = Guid.NewGuid(), UserId = userId, BookId = Guid.NewGuid(), Status = WaitlistStatus.Waiting };
        _waitlistRepository.Setup(r => r.GetByIdAsync(entry.WaitlistId)).ReturnsAsync(entry);

        await _service.CancelWaitlistAsync(userId, entry.WaitlistId);

        _waitlistCascadeService.Verify(c => c.CascadeAsync(It.IsAny<Guid>()), Times.Never);
    }
    
    [Fact]
    public async Task GetMyWaitlistAsync_ReturnsPositionForWaitingEntries()
    {
        var userId = Guid.NewGuid();
        var entry = new Waitlist { WaitlistId = Guid.NewGuid(), UserId = userId, BookId = Guid.NewGuid(), Status = WaitlistStatus.Waiting, JoinedAt = DateTime.UtcNow };
        _waitlistRepository.Setup(r => r.GetUserActiveEntriesAsync(userId)).ReturnsAsync(new List<Waitlist> { entry });
        _waitlistRepository.Setup(r => r.GetWaitingPositionAsync(entry.BookId, entry.JoinedAt)).ReturnsAsync(2);

        var result = await _service.GetMyWaitlistAsync(userId);

        Assert.Equal(2, result.Entries[0].Position);
        Assert.Null(result.Entries[0].ClaimDeadline);
    }

    [Fact]
    public async Task GetMyWaitlistAsync_ReturnsClaimDeadlineForNotifiedEntries()
    {
        var userId = Guid.NewGuid();
        var deadline = DateTime.UtcNow.AddHours(48);
        var entry = new Waitlist { WaitlistId = Guid.NewGuid(), UserId = userId, BookId = Guid.NewGuid(), Status = WaitlistStatus.Notified, ClaimDeadline = deadline };
        _waitlistRepository.Setup(r => r.GetUserActiveEntriesAsync(userId)).ReturnsAsync(new List<Waitlist> { entry });

        var result = await _service.GetMyWaitlistAsync(userId);

        Assert.Equal(deadline, result.Entries[0].ClaimDeadline);
        Assert.Null(result.Entries[0].Position);
    }
}