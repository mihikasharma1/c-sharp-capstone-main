using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using ReservationService.BackgroundJobs;
using ReservationService.Configuration;
using ReservationService.Models;
using ReservationService.Repositories;
using ReservationService.Services;
using Xunit;

namespace ReservationService.Tests.BackgroundJobs;

public class WaitlistExpiryBackgroundServiceTests
{
    [Fact]
    public async Task ProcessExpiredEntriesAsync_ExpiresEntryAndCascades()
    {
        var expiredEntry = new Waitlist
        {
            WaitlistId = Guid.NewGuid(),
            BookId = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Status = WaitlistStatus.Notified,
            ClaimDeadline = DateTime.UtcNow.AddHours(-1)
        };

        var waitlistRepository = new Mock<IWaitlistRepository>();
        waitlistRepository.Setup(r => r.GetExpiredNotifiedEntriesAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Waitlist> { expiredEntry });

        var cascadeService = new Mock<IWaitlistCascadeService>();

        var services = new ServiceCollection();
        services.AddSingleton(waitlistRepository.Object);
        services.AddSingleton(cascadeService.Object);
        var provider = services.BuildServiceProvider();

        var job = new WaitlistExpiryBackgroundService(
            provider,
            Options.Create(new WaitlistExpiryOptions { IntervalMinutes = 60 }),
            NullLogger<WaitlistExpiryBackgroundService>.Instance);

        await job.ProcessExpiredEntriesAsync();

        Assert.Equal(WaitlistStatus.Expired, expiredEntry.Status);
        waitlistRepository.Verify(r => r.SaveChangesAsync(), Times.Once);
        cascadeService.Verify(c => c.CascadeAsync(expiredEntry.BookId), Times.Once);
    }

    [Fact]
    public async Task ProcessExpiredEntriesAsync_DoesNothing_WhenNoExpiredEntries()
    {
        var waitlistRepository = new Mock<IWaitlistRepository>();
        waitlistRepository.Setup(r => r.GetExpiredNotifiedEntriesAsync(It.IsAny<DateTime>()))
            .ReturnsAsync(new List<Waitlist>());

        var cascadeService = new Mock<IWaitlistCascadeService>();

        var services = new ServiceCollection();
        services.AddSingleton(waitlistRepository.Object);
        services.AddSingleton(cascadeService.Object);
        var provider = services.BuildServiceProvider();

        var job = new WaitlistExpiryBackgroundService(
            provider,
            Options.Create(new WaitlistExpiryOptions { IntervalMinutes = 60 }),
            NullLogger<WaitlistExpiryBackgroundService>.Instance);

        await job.ProcessExpiredEntriesAsync();

        cascadeService.Verify(c => c.CascadeAsync(It.IsAny<Guid>()), Times.Never);
    }
}