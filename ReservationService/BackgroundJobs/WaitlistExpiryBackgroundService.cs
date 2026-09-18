using Microsoft.Extensions.Options;
using ReservationService.Configuration;
using ReservationService.Models;
using ReservationService.Repositories;
using ReservationService.Services;

namespace ReservationService.BackgroundJobs;

public class WaitlistExpiryBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly WaitlistExpiryOptions _options;
    private readonly ILogger<WaitlistExpiryBackgroundService> _logger;

    public WaitlistExpiryBackgroundService(
        IServiceProvider serviceProvider,
        IOptions<WaitlistExpiryOptions> options,
        ILogger<WaitlistExpiryBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromMinutes(_options.IntervalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredEntriesAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Waitlist expiry job failed");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    internal async Task ProcessExpiredEntriesAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var waitlistRepository = scope.ServiceProvider.GetRequiredService<IWaitlistRepository>();
        var cascadeService = scope.ServiceProvider.GetRequiredService<IWaitlistCascadeService>();

        var expired = await waitlistRepository.GetExpiredNotifiedEntriesAsync(DateTime.UtcNow);

        if (expired.Count == 0)
        {
            _logger.LogInformation("Waitlist expiry job ran: no expired claims found");
            return;
        }

        _logger.LogInformation("Waitlist expiry job found {Count} expired claim(s)", expired.Count);

        foreach (var entry in expired)
        {
            entry.Status = WaitlistStatus.Expired;
            await waitlistRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Waitlist entry {WaitlistId} for user {UserId} expired (claim deadline passed); cascading book {BookId}",
                entry.WaitlistId, entry.UserId, entry.BookId);

            await cascadeService.CascadeAsync(entry.BookId);
        }
    }
}