using Microsoft.EntityFrameworkCore;
using ReservationService.Data;
using ReservationService.Models;

namespace ReservationService.Repositories;

public interface IWaitlistRepository
{
    Task<Waitlist?> GetNextWaitingEntryAsync(Guid bookId);
    Task<int> GetWaitingPositionAsync(Guid bookId, DateTime joinedAt);
    Task<Waitlist?> GetActiveEntryForUserAndBookAsync(Guid userId, Guid bookId);
    Task<Waitlist?> GetByIdAsync(Guid waitlistId);
    Task<List<Waitlist>> GetUserActiveEntriesAsync(Guid userId);
    Task<List<Waitlist>> GetExpiredNotifiedEntriesAsync(DateTime now);
    Task AddAsync(Waitlist entry);
    Task SaveChangesAsync();
}

public class WaitlistRepository : IWaitlistRepository
{
    private readonly ReservationServiceContext _context;

    public WaitlistRepository(ReservationServiceContext context)
    {
        _context = context;
    }

    public Task<Waitlist?> GetNextWaitingEntryAsync(Guid bookId) =>
        _context.Waitlists
            .Where(w => w.BookId == bookId && w.Status == WaitlistStatus.Waiting)
            .OrderBy(w => w.JoinedAt)
            .FirstOrDefaultAsync();

    public async Task<int> GetWaitingPositionAsync(Guid bookId, DateTime joinedAt)
    {
        var countAhead = await _context.Waitlists.CountAsync(w =>
            w.BookId == bookId && w.Status == WaitlistStatus.Waiting && w.JoinedAt < joinedAt);
        return countAhead + 1;
    }

    public Task<Waitlist?> GetActiveEntryForUserAndBookAsync(Guid userId, Guid bookId) =>
        _context.Waitlists.FirstOrDefaultAsync(w =>
            w.UserId == userId && w.BookId == bookId && w.Status == WaitlistStatus.Waiting);

    public Task<Waitlist?> GetByIdAsync(Guid waitlistId) =>
        _context.Waitlists.FirstOrDefaultAsync(w => w.WaitlistId == waitlistId);

    public Task<List<Waitlist>> GetUserActiveEntriesAsync(Guid userId) =>
        _context.Waitlists
            .Where(w => w.UserId == userId && (w.Status == WaitlistStatus.Waiting || w.Status == WaitlistStatus.Notified))
            .OrderBy(w => w.JoinedAt)
            .ToListAsync();

    public Task<List<Waitlist>> GetExpiredNotifiedEntriesAsync(DateTime now) =>
        _context.Waitlists
            .Where(w => w.Status == WaitlistStatus.Notified && w.ClaimDeadline != null && w.ClaimDeadline < now)
            .ToListAsync();

    public async Task AddAsync(Waitlist entry) => await _context.Waitlists.AddAsync(entry);

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}