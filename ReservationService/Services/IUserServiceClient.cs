using System.Net.Http.Json;

namespace ReservationService.Services;

public class UserValidationDto
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string MembershipStatus { get; set; } = string.Empty;
    public int ActiveReservationsCount { get; set; }
}

public interface IUserServiceClient
{
    Task<UserValidationDto?> ValidateUserAsync(Guid userId);
}

public class UserServiceClient : IUserServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<UserServiceClient> _logger;

    public UserServiceClient(HttpClient httpClient, ILogger<UserServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<UserValidationDto?> ValidateUserAsync(Guid userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/users/{userId}/validate");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<UserValidationDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate user {UserId} via User Service", userId);
            return null;
        }
    }
}