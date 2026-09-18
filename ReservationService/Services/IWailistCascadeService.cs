using ReservationService.Models;
using ReservationService.Repositories;

namespace ReservationService.Services;

public interface IWaitlistCascadeService
{
    Task CascadeAsync(Guid bookId);
}

public class WaitlistCascadeService : IWaitlistCascadeService
{
    private readonly IWaitlistRepository _waitlistRepository;
    private readonly IReservationRepository _reservationRepository;
    private readonly ICatalogServiceClient _catalogServiceClient;
    private readonly ILogger<WaitlistCascadeService> _logger;

    public WaitlistCascadeService(
        IWaitlistRepository waitlistRepository,
        IReservationRepository reservationRepository,
        ICatalogServiceClient catalogServiceClient,
        ILogger<WaitlistCascadeService> logger)
    {
        _waitlistRepository = waitlistRepository;
        _reservationRepository = reservationRepository;
        _catalogServiceClient = catalogServiceClient;
        _logger = logger;
    }

    public async Task CascadeAsync(Guid bookId)
    {
        while (true)
        {
            var nextEntry = await _waitlistRepository.GetNextWaitingEntryAsync(bookId);
            if (nextEntry is null)
            {
                await _catalogServiceClient.UpdateAvailabilityAsync(bookId, 1);
                _logger.LogInformation("No eligible waitlist entry for book {BookId}; copy released to general availability", bookId);
                return;
            }

            var activeCount = await _reservationRepository.GetActiveCountAsync(nextEntry.UserId);
            if (activeCount >= 5)
            {
                nextEntry.Status = WaitlistStatus.Expired;
                await _waitlistRepository.SaveChangesAsync();
                _logger.LogInformation(
                    "Waitlist entry {WaitlistId} for user {UserId} skipped (at reservation limit)",
                    nextEntry.WaitlistId, nextEntry.UserId);
                continue;
            }

            var reservation = new Reservation
            {
                BookId = bookId,
                UserId = nextEntry.UserId,
                Status = ReservationStatus.Reserved,
                ReservedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(7),
                BookTitle = nextEntry.BookTitle,
                BookAuthor = nextEntry.BookAuthor
            };
            await _reservationRepository.AddAsync(reservation);

            nextEntry.Status = WaitlistStatus.Notified;
            nextEntry.NotifiedAt = DateTime.UtcNow;
            nextEntry.ClaimDeadline = DateTime.UtcNow.AddHours(48);
            nextEntry.ResultingReservationId = reservation.ReservationId;

            await _reservationRepository.SaveChangesAsync();

            _logger.LogInformation(
                "Book {BookId} offered to waitlist entry {WaitlistId} (user {UserId}); claim deadline {ClaimDeadline}",
                bookId, nextEntry.WaitlistId, nextEntry.UserId, nextEntry.ClaimDeadline);
            return;
        }
    }
}