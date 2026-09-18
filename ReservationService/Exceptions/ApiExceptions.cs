namespace ReservationService.Exceptions;

public abstract class ApiException : Exception
{
    public abstract string ErrorCode { get; }
    public abstract int StatusCode { get; }
    protected ApiException(string message) : base(message) { }
}

public class NotFoundException : ApiException
{
    public override string ErrorCode => "NOT_FOUND";
    public override int StatusCode => StatusCodes.Status404NotFound;
    public NotFoundException(string message) : base(message) { }
}

public class ForbiddenException : ApiException
{
    public override string ErrorCode => "FORBIDDEN";
    public override int StatusCode => StatusCodes.Status403Forbidden;
    public ForbiddenException(string message) : base(message) { }
}

public class UnauthorizedApiException : ApiException
{
    public override string ErrorCode => "UNAUTHORIZED";
    public override int StatusCode => StatusCodes.Status401Unauthorized;
    public UnauthorizedApiException(string message) : base(message) { }
}

public class InvalidStatusException : ApiException
{
    public override string ErrorCode => "INVALID_STATUS";
    public override int StatusCode => StatusCodes.Status400BadRequest;
    public string CurrentStatus { get; }
    public InvalidStatusException(string message, string currentStatus) : base(message) => CurrentStatus = currentStatus;
}

public class ReservationLimitExceededException : ApiException
{
    public override string ErrorCode => "RESERVATION_LIMIT_EXCEEDED";
    public override int StatusCode => StatusCodes.Status400BadRequest;
    public int CurrentReservations { get; }
    public ReservationLimitExceededException(string message, int currentReservations) : base(message) => CurrentReservations = currentReservations;
}

public class BookUnavailableException : ApiException
{
    public override string ErrorCode => "BOOK_UNAVAILABLE";
    public override int StatusCode => StatusCodes.Status400BadRequest;
    public int AvailableCopies { get; }
    public BookUnavailableException(string message, int availableCopies) : base(message) => AvailableCopies = availableCopies;
}

public class BookAvailableException : ApiException
{
    public override string ErrorCode => "BOOK_AVAILABLE";
    public override int StatusCode => StatusCodes.Status400BadRequest;
    public BookAvailableException(string message) : base(message) { }
}

public class AlreadyWaitlistedException : ApiException
{
    public override string ErrorCode => "ALREADY_WAITLISTED";
    public override int StatusCode => StatusCodes.Status400BadRequest;
    public AlreadyWaitlistedException(string message) : base(message) { }
}