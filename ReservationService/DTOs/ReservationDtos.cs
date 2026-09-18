using System.ComponentModel.DataAnnotations;

namespace ReservationService.DTOs;

public class ReservationCreateRequestDto
{
    [Required]
    public Guid BookId { get; set; }
}

public class ReservationResponseDto
{
    public Guid ReservationId { get; set; }
    public Guid BookId { get; set; }
    public Guid UserId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime ReservedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ActiveReservationDto
{
    public Guid ReservationId { get; set; }
    public Guid BookId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string BookAuthor { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? ReservedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public int? DaysUntilExpiry { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public DateTime? DueDate { get; set; }
    public int? DaysUntilDue { get; set; }
}

public class ActiveReservationsResponseDto
{
    public List<ActiveReservationDto> Reservations { get; set; } = new();
    public int TotalActive { get; set; }
}

public class CheckoutRequestDto
{
    public string? Notes { get; set; }
}

public class CheckoutResponseDto
{
    public Guid ReservationId { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CheckedOutAt { get; set; }
    public DateTime DueDate { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class ReturnRequestDto
{
    [Required]
    public string Condition { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class ReturnResponseDto
{
    public Guid ReservationId { get; set; }
    public DateTime ReturnedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public int LateDays { get; set; }
    public decimal LateFee { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class HistoryRecordDto
{
    public Guid ReservationId { get; set; }
    public string BookTitle { get; set; } = string.Empty;
    public string BookAuthor { get; set; } = string.Empty;
    public DateTime ReservedAt { get; set; }
    public DateTime? CheckedOutAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool WasLate { get; set; }
}

public class StatisticsDto
{
    public Guid UserId { get; set; }
    public int ActiveReservations { get; set; }
    public int BorrowingHistory { get; set; }
}

public class PagedResultDto<T>
{
    public List<T> Content { get; set; } = new();
    public int Page { get; set; }
    public int Size { get; set; }
    public int TotalElements { get; set; }
    public int TotalPages { get; set; }
    public bool Last { get; set; }
}