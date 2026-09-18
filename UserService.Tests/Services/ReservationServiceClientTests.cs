using System.Net;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using UserService.Services;
using Xunit;

namespace UserService.Tests.Services;

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

public class ReservationServiceClientTests
{
    [Fact]
    public async Task GetStatisticsAsync_ReturnsStats_WhenSuccessful()
    {
        var userId = Guid.NewGuid();
        var json = $$"""{"userId":"{{userId}}","activeReservations":2,"borrowingHistory":5}""";
        var httpClient = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.OK, json)) { BaseAddress = new Uri("http://localhost") };
        var client = new ReservationServiceClient(httpClient, NullLogger<ReservationServiceClient>.Instance);

        var result = await client.GetStatisticsAsync(userId);

        Assert.NotNull(result);
        Assert.Equal(2, result!.ActiveReservations);
    }

    [Fact]
    public async Task GetStatisticsAsync_ReturnsNull_WhenServiceUnavailable()
    {
        var httpClient = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.ServiceUnavailable)) { BaseAddress = new Uri("http://localhost") };
        var client = new ReservationServiceClient(httpClient, NullLogger<ReservationServiceClient>.Instance);

        Assert.Null(await client.GetStatisticsAsync(Guid.NewGuid()));
    }
}