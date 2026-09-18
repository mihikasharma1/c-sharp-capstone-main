using Microsoft.EntityFrameworkCore;
using ReservationService.Data;
using ReservationService.Models;
using ReservationService.Repositories;
using Xunit;

namespace ReservationService.Tests.Repositories;

public class ReservationRepositoryTests
{
    private static ReservationServiceContext CreateContext() =>
        new(new DbContextOptionsBuilder<ReservationServiceContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    [Fact]
    public async Task GetActiveCountAsync_CountsOnlyReservedAndCheckedOut()
    {
        var context = CreateContext();
        var userId = Guid.NewGuid();
        context.Reservations.AddRange(
            new Reservation { UserId = userId, Status = ReservationStatus.Reserved, BookTitle = "A", BookAuthor = "A" },
            new Reservation { UserId = userId, Status = ReservationStatus.CheckedOut, BookTitle = "B", BookAuthor = "B" },
            new Reservation { UserId = userId, Status = ReservationStatus.Returned, BookTitle = "C", BookAuthor = "C" }
        );
        await context.SaveChangesAsync();

        Assert.Equal(2, await new ReservationRepository(context).GetActiveCountAsync(userId));
    }

    [Fact]
    public async Task GetHistoryAsync_SortsByMostRecentFirst()
    {
        var context = CreateContext();
        var userId = Guid.NewGuid();
        context.Reservations.AddRange(
            new Reservation { UserId = userId, Status = ReservationStatus.Returned, ReservedAt = DateTime.UtcNow.AddDays(-10), ReturnedAt = DateTime.UtcNow.AddDays(-5), BookTitle = "Old", BookAuthor = "X" },
            new Reservation { UserId = userId, Status = ReservationStatus.Returned, ReservedAt = DateTime.UtcNow.AddDays(-2), ReturnedAt = DateTime.UtcNow.AddDays(-1), BookTitle = "New", BookAuthor = "Y" }
        );
        await context.SaveChangesAsync();

        var (items, total) = await new ReservationRepository(context).GetHistoryAsync(userId, 0, 20);

        Assert.Equal(2, total);
        Assert.Equal("New", items[0].BookTitle);
    }

    [Fact]
    public async Task GetHistoryAsync_Paginates()
    {
        var context = CreateContext();
        var userId = Guid.NewGuid();
        for (var i = 0; i < 5; i++)
            context.Reservations.Add(new Reservation { UserId = userId, Status = ReservationStatus.Returned, ReservedAt = DateTime.UtcNow.AddDays(-i), BookTitle = $"Book{i}", BookAuthor = "X" });
        await context.SaveChangesAsync();

        var (items, total) = await new ReservationRepository(context).GetHistoryAsync(userId, 1, 2);

        Assert.Equal(5, total);
        Assert.Equal(2, items.Count);
    }
}