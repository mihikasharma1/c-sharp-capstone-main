using System.Net.Http.Json;

namespace UserService.Services;

public class ReservationStatisticsDto
{
    public Guid UserId { get; set; }
    public int ActiveReservations { get; set; }
    public int BorrowingHistory { get; set; }
}

public interface IReservationServiceClient
{
    Task<ReservationStatisticsDto?> GetStatisticsAsync(Guid userId);
}

public class ReservationServiceClient : IReservationServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ReservationServiceClient> _logger;

    public ReservationServiceClient(HttpClient httpClient, ILogger<ReservationServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<ReservationStatisticsDto?> GetStatisticsAsync(Guid userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/reservations/statistics/{userId}");
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Reservation Service returned {StatusCode} for user {UserId}",
                    response.StatusCode, userId);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<ReservationStatisticsDto>();
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Reservation Service unavailable while fetching statistics for user {UserId}", userId);
            return null;
        }
    }
}