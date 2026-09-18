using System.Text.Json;
using ReservationService.DTOs;
using ReservationService.Exceptions;

namespace ReservationService.Middleware;

public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var correlationId = Guid.NewGuid().ToString();

            object payload = ex switch
            {
                ReservationLimitExceededException e => new ReservationLimitExceededDto { Message = e.Message, CurrentReservations = e.CurrentReservations },
                BookUnavailableException e => new BookUnavailableDto { Message = e.Message, AvailableCopies = e.AvailableCopies },
                InvalidStatusException e => new InvalidStatusDto { Message = e.Message, CurrentStatus = e.CurrentStatus },
                ApiException e => new ErrorResponseDto { Error = e.ErrorCode, Message = e.Message },
                _ => new ErrorResponseDto { Error = "INTERNAL_SERVER_ERROR", Message = "An unexpected error occurred" }
            };

            var statusCode = ex is ApiException apiEx ? apiEx.StatusCode : StatusCodes.Status500InternalServerError;

            if (statusCode == StatusCodes.Status500InternalServerError)
                _logger.LogError(ex, "Unhandled exception. CorrelationId: {CorrelationId}", correlationId);
            else
                _logger.LogWarning("Handled API exception {ErrorCode}: {Message}. CorrelationId: {CorrelationId}",
                    (ex as ApiException)?.ErrorCode, ex.Message, correlationId);

            context.Response.ContentType = "application/json";
            context.Response.StatusCode = statusCode;
            await context.Response.WriteAsync(JsonSerializer.Serialize(payload, JsonOptions));
        }
    }
}