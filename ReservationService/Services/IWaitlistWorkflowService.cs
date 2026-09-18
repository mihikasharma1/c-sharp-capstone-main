using ReservationService.DTOs;
using ReservationService.Exceptions;
using ReservationService.Models;
using ReservationService.Repositories;

namespace ReservationService.Services;

public interface IWaitlistWorkflowService
{
    Task<WaitlistResponseDto> JoinWaitlistAsync(Guid userId, Guid bookId);
    Task<WaitlistEntriesResponseDto> GetMyWaitlistAsync(Guid userId);
    Task<WaitlistCancelResponseDto> CancelWaitlistAsync(Guid userId, Guid waitlistId);
}

public class WaitlistWorkflowService : IWaitlistWorkflowService
{
    private readonly IWaitlistRepository _waitlistRepository;
    private readonly ICatalogServiceClient _catalogServiceClient;
    private readonly IWaitlistCascadeService _waitlistCascadeService;

    public WaitlistWorkflowService(
        IWaitlistRepository waitlistRepository,
        ICatalogServiceClient catalogServiceClient,
        IWaitlistCascadeService waitlistCascadeService)
    {
        _waitlistRepository = waitlistRepository;
        _catalogServiceClient = catalogServiceClient;
        _waitlistCascadeService = waitlistCascadeService;
    }

    public async Task<WaitlistResponseDto> JoinWaitlistAsync(Guid userId, Guid bookId)
    {
        var book = await _catalogServiceClient.GetBookAsync(bookId);
        if (book is null)
            throw new NotFoundException($"Book not found with ID: {bookId}");

        if (book.AvailableCopies > 0)
            throw new BookAvailableException("This book currently has available copies - reserve it directly instead of joining the waitlist");

        var existing = await _waitlistRepository.GetActiveEntryForUserAndBookAsync(userId, bookId);
        if (existing is not null)
            throw new AlreadyWaitlistedException("You are already on the waitlist for this book");

        var entry = new Waitlist
        {
            BookId = bookId,
            UserId = userId,
            Status = WaitlistStatus.Waiting,
            JoinedAt = DateTime.UtcNow,
            BookTitle = book.Title,
            BookAuthor = book.Author
        };

        await _waitlistRepository.AddAsync(entry);
        await _waitlistRepository.SaveChangesAsync();

        var position = await _waitlistRepository.GetWaitingPositionAsync(bookId, entry.JoinedAt);

        return new WaitlistResponseDto
        {
            WaitlistId = entry.WaitlistId,
            BookId = entry.BookId,
            BookTitle = entry.BookTitle,
            Status = entry.Status.ToString().ToUpperInvariant(),
            JoinedAt = entry.JoinedAt,
            Position = position
        };
    }

    public async Task<WaitlistEntriesResponseDto> GetMyWaitlistAsync(Guid userId)
    {
        var entries = await _waitlistRepository.GetUserActiveEntriesAsync(userId);
        var dtos = new List<WaitlistEntryDto>();

        foreach (var entry in entries)
        {
            var dto = new WaitlistEntryDto
            {
                WaitlistId = entry.WaitlistId,
                BookId = entry.BookId,
                BookTitle = entry.BookTitle,
                BookAuthor = entry.BookAuthor,
                Status = entry.Status.ToString().ToUpperInvariant(),
                JoinedAt = entry.JoinedAt
            };

            if (entry.Status == WaitlistStatus.Waiting)
                dto.Position = await _waitlistRepository.GetWaitingPositionAsync(entry.BookId, entry.JoinedAt);
            else if (entry.Status == WaitlistStatus.Notified)
            {
                dto.NotifiedAt = entry.NotifiedAt;
                dto.ClaimDeadline = entry.ClaimDeadline;
            }

            dtos.Add(dto);
        }

        return new WaitlistEntriesResponseDto { Entries = dtos };
    }

    public async Task<WaitlistCancelResponseDto> CancelWaitlistAsync(Guid userId, Guid waitlistId)
    {
        var entry = await _waitlistRepository.GetByIdAsync(waitlistId);
        if (entry is null || entry.UserId != userId)
            throw new NotFoundException("Waitlist entry not found");

        var wasNotified = entry.Status == WaitlistStatus.Notified;
        entry.Status = WaitlistStatus.Cancelled;
        await _waitlistRepository.SaveChangesAsync();

        if (wasNotified)
            await _waitlistCascadeService.CascadeAsync(entry.BookId);

        return new WaitlistCancelResponseDto
        {
            WaitlistId = entry.WaitlistId,
            Status = "CANCELLED",
            Message = "You have been removed from the waitlist"
        };
    }
}