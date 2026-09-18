using System.Net.Http.Json;

namespace ReservationService.Services;

public class CatalogBookDto
{
    public Guid BookId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public int TotalCopies { get; set; }
    public int AvailableCopies { get; set; }
    public string Status { get; set; } = string.Empty;
}

public interface ICatalogServiceClient
{
    Task<CatalogBookDto?> GetBookAsync(Guid bookId);
    Task<bool> UpdateAvailabilityAsync(Guid bookId, int delta);
}

public class CatalogServiceClient : ICatalogServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CatalogServiceClient> _logger;

    public CatalogServiceClient(HttpClient httpClient, ILogger<CatalogServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<CatalogBookDto?> GetBookAsync(Guid bookId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/catalog/books/{bookId}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<CatalogBookDto>();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch book {BookId} from Catalog Service", bookId);
            return null;
        }
    }

    public async Task<bool> UpdateAvailabilityAsync(Guid bookId, int delta)
    {
        try
        {
            var response = await _httpClient.PutAsJsonAsync($"/api/catalog/books/{bookId}/availability", new { delta });
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update availability for book {BookId}", bookId);
            return false;
        }
    }
}