using Microsoft.EntityFrameworkCore;
using ReservationService.Data;
using ReservationService.Models;
using ReservationService.Repositories;
using Xunit;

namespace ReservationService.Tests.Repositories;

public class WaitlistRepositoryTests
{
    private static ReservationServiceContext CreateContext() =>
        new(new DbContextOptionsBuilder<ReservationServiceContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task GetWaitingPositionAsync_ReturnsOneIndexedPosition()
    {
        var context = CreateContext();
        var bookId = Guid.NewGuid();
        var baseTime = DateTime.UtcNow;
        context.Waitlists.AddRange(
            new Waitlist { BookId = bookId, UserId = Guid.NewGuid(), Status = WaitlistStatus.Waiting, JoinedAt = baseTime },
            new Waitlist { BookId = bookId, UserId = Guid.NewGuid(), Status = WaitlistStatus.Waiting, JoinedAt = baseTime.AddMinutes(1) },
            new Waitlist { BookId = bookId, UserId = Guid.NewGuid(), Status = WaitlistStatus.Waiting, JoinedAt = baseTime.AddMinutes(2) }
        );
        await context.SaveChangesAsync();

        var position = await new WaitlistRepository(context).GetWaitingPositionAsync(bookId, baseTime.AddMinutes(2));

        Assert.Equal(3, position);
    }

    [Fact]
    public async Task GetNextWaitingEntryAsync_ReturnsLongestWaitingEntry()
    {
        var context = CreateContext();
        var bookId = Guid.NewGuid();
        var older = new Waitlist { BookId = bookId, UserId = Guid.NewGuid(), Status = WaitlistStatus.Waiting, JoinedAt = DateTime.UtcNow.AddHours(-1) };
        context.Waitlists.AddRange(older, new Waitlist { BookId = bookId, UserId = Guid.NewGuid(), Status = WaitlistStatus.Waiting, JoinedAt = DateTime.UtcNow });
        await context.SaveChangesAsync();

        var result = await new WaitlistRepository(context).GetNextWaitingEntryAsync(bookId);

        Assert.Equal(older.WaitlistId, result!.WaitlistId);
    }

    [Fact]
    public async Task GetExpiredNotifiedEntriesAsync_ReturnsOnlyPastDeadlines()
    {
        var context = CreateContext();
        context.Waitlists.AddRange(
            new Waitlist { BookId = Guid.NewGuid(), UserId = Guid.NewGuid(), Status = WaitlistStatus.Notified, JoinedAt = DateTime.UtcNow, ClaimDeadline = DateTime.UtcNow.AddHours(-1) },
            new Waitlist { BookId = Guid.NewGuid(), UserId = Guid.NewGuid(), Status = WaitlistStatus.Notified, JoinedAt = DateTime.UtcNow, ClaimDeadline = DateTime.UtcNow.AddHours(1) }
        );
        await context.SaveChangesAsync();

        var expired = await new WaitlistRepository(context).GetExpiredNotifiedEntriesAsync(DateTime.UtcNow);

        Assert.Single(expired);
    }
}