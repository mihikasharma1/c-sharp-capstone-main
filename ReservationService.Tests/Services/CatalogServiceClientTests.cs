using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using ReservationService.Services;
using Xunit;

namespace ReservationService.Tests.Services;

public class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly HttpStatusCode _statusCode;
    private readonly string? _jsonContent;

    public FakeHttpMessageHandler(HttpStatusCode statusCode, string? jsonContent = null)
    {
        _statusCode = statusCode;
        _jsonContent = jsonContent;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = new HttpResponseMessage(_statusCode);
        if (_jsonContent is not null)
            response.Content = new StringContent(_jsonContent, Encoding.UTF8, "application/json");
        return Task.FromResult(response);
    }
}

public class CatalogServiceClientTests
{
    [Fact]
    public async Task GetBookAsync_ReturnsBook_WhenResponseIsSuccessful()
    {
        var bookId = Guid.NewGuid();
        var json = $$"""{"bookId":"{{bookId}}","title":"Clean Code","author":"Robert Martin","totalCopies":5,"availableCopies":2,"status":"AVAILABLE"}""";
        var httpClient = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.OK, json)) { BaseAddress = new Uri("http://localhost") };
        var client = new CatalogServiceClient(httpClient, NullLogger<CatalogServiceClient>.Instance);

        var result = await client.GetBookAsync(bookId);

        Assert.NotNull(result);
        Assert.Equal("Clean Code", result!.Title);
    }

    [Fact]
    public async Task GetBookAsync_ReturnsNull_WhenNotFound()
    {
        var httpClient = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.NotFound)) { BaseAddress = new Uri("http://localhost") };
        var client = new CatalogServiceClient(httpClient, NullLogger<CatalogServiceClient>.Instance);

        Assert.Null(await client.GetBookAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task UpdateAvailabilityAsync_ReturnsTrue_WhenSuccessful()
    {
        var httpClient = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.OK)) { BaseAddress = new Uri("http://localhost") };
        var client = new CatalogServiceClient(httpClient, NullLogger<CatalogServiceClient>.Instance);

        Assert.True(await client.UpdateAvailabilityAsync(Guid.NewGuid(), -1));
    }

    [Fact]
    public async Task UpdateAvailabilityAsync_ReturnsFalse_WhenRequestFails()
    {
        var httpClient = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.BadRequest)) { BaseAddress = new Uri("http://localhost") };
        var client = new CatalogServiceClient(httpClient, NullLogger<CatalogServiceClient>.Instance);

        Assert.False(await client.UpdateAvailabilityAsync(Guid.NewGuid(), -1));
    }
}