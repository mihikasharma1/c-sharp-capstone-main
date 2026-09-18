using ReservationService.DTOs;
using ReservationService.Exceptions;
using ReservationService.Models;
using ReservationService.Repositories;

namespace ReservationService.Services;

public interface IReservationWorkflowService
{
    Task<ReservationResponseDto> CreateReservationAsync(Guid userId, Guid bookId);
    Task<ActiveReservationsResponseDto> GetActiveReservationsAsync(Guid userId);
    Task<CheckoutResponseDto> CheckoutAsync(Guid reservationId, string? notes);
    Task<ReturnResponseDto> ReturnAsync(Guid reservationId, string condition, string? notes);
    Task<PagedResultDto<HistoryRecordDto>> GetHistoryAsync(Guid userId, int page, int size);
    Task<StatisticsDto> GetStatisticsAsync(Guid userId);
}

public class ReservationWorkflowService : IReservationWorkflowService
{
    private const int MaxActiveReservations = 5;
    private const int ReservationExpiryDays = 7;
    private const int CheckoutPeriodDays = 14;
    private const decimal LateFeePerDay = 1.00m;

    private readonly IReservationRepository _reservationRepository;
    private readonly ICatalogServiceClient _catalogServiceClient;
    private readonly IUserServiceClient _userServiceClient;
    private readonly IWaitlistCascadeService _waitlistCascadeService;

    public ReservationWorkflowService(
        IReservationRepository reservationRepository,
        ICatalogServiceClient catalogServiceClient,
        IUserServiceClient userServiceClient,
        IWaitlistCascadeService waitlistCascadeService)
    {
        _reservationRepository = reservationRepository;
        _catalogServiceClient = catalogServiceClient;
        _userServiceClient = userServiceClient;
        _waitlistCascadeService = waitlistCascadeService;
    }

    public async Task<ReservationResponseDto> CreateReservationAsync(Guid userId, Guid bookId)
    {
        var userValidation = await _userServiceClient.ValidateUserAsync(userId);
        if (userValidation is null)
            throw new Exception("Unable to validate user via User Service");
        if (userValidation.MembershipStatus != "ACTIVE")
            throw new ForbiddenException("Your membership is not active");

        var activeCount = await _reservationRepository.GetActiveCountAsync(userId);
        if (activeCount >= MaxActiveReservations)
            throw new ReservationLimitExceededException("You have reached the maximum of 5 active reservations", activeCount);

        var book = await _catalogServiceClient.GetBookAsync(bookId);
        if (book is null)
            throw new NotFoundException($"Book not found with ID: {bookId}");
        if (book.AvailableCopies <= 0)
            throw new BookUnavailableException("No copies available for reservation", book.AvailableCopies);

        var updated = await _catalogServiceClient.UpdateAvailabilityAsync(bookId, -1);
        if (!updated)
            throw new Exception("Failed to update book availability in Catalog Service");

        var reservation = new Reservation
        {
            BookId = bookId,
            UserId = userId,
            Status = ReservationStatus.Reserved,
            ReservedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(ReservationExpiryDays),
            BookTitle = book.Title,
            BookAuthor = book.Author
        };

        await _reservationRepository.AddAsync(reservation);
        await _reservationRepository.SaveChangesAsync();

        return new ReservationResponseDto
        {
            ReservationId = reservation.ReservationId,
            BookId = reservation.BookId,
            UserId = reservation.UserId,
            BookTitle = reservation.BookTitle,
            Status = ToContractString(reservation.Status),
            ReservedAt = reservation.ReservedAt,
            ExpiresAt = reservation.ExpiresAt,
            Message = "Book reserved successfully. Please pick up within 7 days."
        };
    }

    public async Task<ActiveReservationsResponseDto> GetActiveReservationsAsync(Guid userId)
    {
        var reservations = await _reservationRepository.GetActiveReservationsAsync(userId);
        var now = DateTime.UtcNow;

        var dtos = reservations.Select(r => new ActiveReservationDto
        {
            ReservationId = r.ReservationId,
            BookId = r.BookId,
            BookTitle = r.BookTitle,
            BookAuthor = r.BookAuthor,
            Status = ToContractString(r.Status),
            ReservedAt = r.Status == ReservationStatus.Reserved ? r.ReservedAt : null,
            ExpiresAt = r.Status == ReservationStatus.Reserved ? r.ExpiresAt : null,
            DaysUntilExpiry = r.Status == ReservationStatus.Reserved && r.ExpiresAt.HasValue
                ? Math.Max(0, (int)Math.Ceiling((r.ExpiresAt.Value - now).TotalDays)) : null,
            CheckedOutAt = r.Status == ReservationStatus.CheckedOut ? r.CheckedOutAt : null,
            DueDate = r.Status == ReservationStatus.CheckedOut ? r.DueDate : null,
            DaysUntilDue = r.Status == ReservationStatus.CheckedOut && r.DueDate.HasValue
                ? Math.Max(0, (int)Math.Ceiling((r.DueDate.Value - now).TotalDays)) : null
        }).ToList();

        return new ActiveReservationsResponseDto { Reservations = dtos, TotalActive = dtos.Count };
    }

    public async Task<CheckoutResponseDto> CheckoutAsync(Guid reservationId, string? notes)
    {
        var reservation = await _reservationRepository.GetByIdAsync(reservationId);
        if (reservation is null)
            throw new NotFoundException($"Reservation not found with ID: {reservationId}");

        if (reservation.Status != ReservationStatus.Reserved)
            throw new InvalidStatusException(
                "Can only checkout reservations with RESERVED status",
                ToContractString(reservation.Status));

        reservation.Status = ReservationStatus.CheckedOut;
        reservation.CheckedOutAt = DateTime.UtcNow;
        reservation.DueDate = reservation.CheckedOutAt.Value.AddDays(CheckoutPeriodDays);
        reservation.Notes = notes;

        await _reservationRepository.SaveChangesAsync();

        return new CheckoutResponseDto
        {
            ReservationId = reservation.ReservationId,
            Status = ToContractString((reservation.Status)),
            CheckedOutAt = reservation.CheckedOutAt.Value,
            DueDate = reservation.DueDate.Value,
            Message = $"Book checked out successfully. Due date: {reservation.DueDate.Value:MMMM d, yyyy}"
        };
    }

    public async Task<ReturnResponseDto> ReturnAsync(Guid reservationId, string condition, string? notes)
    {
        var reservation = await _reservationRepository.GetByIdAsync(reservationId);
        if (reservation is null)
            throw new NotFoundException($"Reservation not found with ID: {reservationId}");

        if (reservation.Status != ReservationStatus.CheckedOut)
            throw new InvalidStatusException(
                "Can only return books with CHECKED_OUT status",
                ToContractString((reservation.Status)));

        if (!Enum.TryParse<BookCondition>(condition, true, out var parsedCondition))
            throw new InvalidStatusException("Invalid condition value", condition);

        var now = DateTime.UtcNow;
        reservation.Status = ReservationStatus.Returned;
        reservation.ReturnedAt = now;
        reservation.Condition = parsedCondition;
        reservation.Notes = notes;

        var lateDays = 0;
        var lateFee = 0m;
        if (reservation.DueDate.HasValue && now > reservation.DueDate.Value)
        {
            lateDays = (int)Math.Ceiling((now - reservation.DueDate.Value).TotalDays);
            lateFee = lateDays * LateFeePerDay;
        }
        reservation.LateDays = lateDays;
        reservation.LateFee = lateFee;

        await _reservationRepository.SaveChangesAsync();

        // Decides internally: hand the copy to the next eligible waitlist entry, or release it back to general availability
        await _waitlistCascadeService.CascadeAsync(reservation.BookId);

        var message = lateDays > 0
            ? $"Book returned. Late fee of ${lateFee:F2} applied to account."
            : "Book returned successfully";

        return new ReturnResponseDto
        {
            ReservationId = reservation.ReservationId,
            ReturnedAt = reservation.ReturnedAt.Value,
            DueDate = reservation.DueDate,
            LateDays = lateDays,
            LateFee = lateFee,
            Message = message
        };
    }

    public async Task<PagedResultDto<HistoryRecordDto>> GetHistoryAsync(Guid userId, int page, int size)
    {
        var (items, totalCount) = await _reservationRepository.GetHistoryAsync(userId, page, size);

        var content = items.Select(r => new HistoryRecordDto
        {
            ReservationId = r.ReservationId,
            BookTitle = r.BookTitle,
            BookAuthor = r.BookAuthor,
            ReservedAt = r.ReservedAt,
            CheckedOutAt = r.CheckedOutAt,
            ReturnedAt = r.ReturnedAt,
            DueDate = r.DueDate,
            Status = ToContractString(r.Status),
            WasLate = r.ReturnedAt.HasValue && r.DueDate.HasValue && r.ReturnedAt.Value > r.DueDate.Value
        }).ToList();

        var totalPages = totalCount == 0 ? 0 : (int)Math.Ceiling(totalCount / (double)size);

        return new PagedResultDto<HistoryRecordDto>
        {
            Content = content, Page = page, Size = size,
            TotalElements = totalCount, TotalPages = totalPages, Last = page >= totalPages - 1
        };
    }

    public async Task<StatisticsDto> GetStatisticsAsync(Guid userId)
    {
        var active = await _reservationRepository.GetActiveCountAsync(userId);
        var completed = await _reservationRepository.GetCompletedCountAsync(userId);
        return new StatisticsDto { UserId = userId, ActiveReservations = active, BorrowingHistory = completed };
    }
    
    private static string ToContractString(ReservationStatus status) => status switch
    {
        ReservationStatus.Reserved => "RESERVED",
        ReservationStatus.CheckedOut => "CHECKED_OUT",
        ReservationStatus.Returned => "RETURNED",
        ReservationStatus.Cancelled => "CANCELLED",
        _ => status.ToString().ToUpperInvariant()
    };
}