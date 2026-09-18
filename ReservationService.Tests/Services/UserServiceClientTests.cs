using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using ReservationService.Services;
using Xunit;

namespace ReservationService.Tests.Services;

public class UserServiceClientTests
{
    [Fact]
    public async Task ValidateUserAsync_ReturnsValidation_WhenSuccessful()
    {
        var userId = Guid.NewGuid();
        var json = $$"""{"userId":"{{userId}}","email":"a@b.com","firstName":"A","lastName":"B","role":"PATRON","membershipStatus":"ACTIVE","activeReservationsCount":1}""";
        var httpClient = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.OK, json)) { BaseAddress = new Uri("http://localhost") };
        var client = new UserServiceClient(httpClient, NullLogger<UserServiceClient>.Instance);

        var result = await client.ValidateUserAsync(userId);

        Assert.NotNull(result);
        Assert.Equal("ACTIVE", result!.MembershipStatus);
    }

    [Fact]
    public async Task ValidateUserAsync_ReturnsNull_WhenNotFound()
    {
        var httpClient = new HttpClient(new FakeHttpMessageHandler(HttpStatusCode.NotFound)) { BaseAddress = new Uri("http://localhost") };
        var client = new UserServiceClient(httpClient, NullLogger<UserServiceClient>.Instance);

        Assert.Null(await client.ValidateUserAsync(Guid.NewGuid()));
    }
}