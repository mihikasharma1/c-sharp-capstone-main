using Microsoft.EntityFrameworkCore;
using ReservationService.Data;
using ReservationService.Models;

namespace ReservationService.Repositories;

public interface IReservationRepository
{
    Task<int> GetActiveCountAsync(Guid userId);
    Task<int> GetCompletedCountAsync(Guid userId);
    Task<List<Reservation>> GetActiveReservationsAsync(Guid userId);
    Task<Reservation?> GetByIdAsync(Guid reservationId);
    Task AddAsync(Reservation reservation);
    Task<(List<Reservation> Items, int TotalCount)> GetHistoryAsync(Guid userId, int page, int size);
    Task SaveChangesAsync();
}

public class ReservationRepository : IReservationRepository
{
    private readonly ReservationServiceContext _context;

    public ReservationRepository(ReservationServiceContext context)
    {
        _context = context;
    }

    public Task<int> GetActiveCountAsync(Guid userId) =>
        _context.Reservations.CountAsync(r =>
            r.UserId == userId &&
            (r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.CheckedOut));

    public Task<int> GetCompletedCountAsync(Guid userId) =>
        _context.Reservations.CountAsync(r => r.UserId == userId && r.Status == ReservationStatus.Returned);

    public Task<List<Reservation>> GetActiveReservationsAsync(Guid userId) =>
        _context.Reservations
            .Where(r => r.UserId == userId &&
                (r.Status == ReservationStatus.Reserved || r.Status == ReservationStatus.CheckedOut))
            .ToListAsync();

    public Task<Reservation?> GetByIdAsync(Guid reservationId) =>
        _context.Reservations.FirstOrDefaultAsync(r => r.ReservationId == reservationId);

    public async Task AddAsync(Reservation reservation) => await _context.Reservations.AddAsync(reservation);

    public async Task<(List<Reservation> Items, int TotalCount)> GetHistoryAsync(Guid userId, int page, int size)
    {
        var query = _context.Reservations.Where(r => r.UserId == userId);
        var totalCount = await query.CountAsync();

        var items = await query
            .OrderByDescending(r => r.ReturnedAt ?? r.ReservedAt)
            .Skip(page * size)
            .Take(size)
            .ToListAsync();

        return (items, totalCount);
    }

    public Task SaveChangesAsync() => _context.SaveChangesAsync();
}