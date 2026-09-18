namespace ReservationService.DTOs;

public class ErrorResponseDto
{
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class ReservationLimitExceededDto
{
    public string Error { get; set; } = "RESERVATION_LIMIT_EXCEEDED";
    public string Message { get; set; } = string.Empty;
    public int CurrentReservations { get; set; }
}

public class BookUnavailableDto
{
    public string Error { get; set; } = "BOOK_UNAVAILABLE";
    public string Message { get; set; } = string.Empty;
    public int AvailableCopies { get; set; }
}

public class InvalidStatusDto
{
    public string Error { get; set; } = "INVALID_STATUS";
    public string Message { get; set; } = string.Empty;
    public string CurrentStatus { get; set; } = string.Empty;
}