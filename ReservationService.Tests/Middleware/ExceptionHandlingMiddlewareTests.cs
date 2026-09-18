using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging.Abstractions;
using ReservationService.Exceptions;
using ReservationService.Middleware;
using Xunit;

namespace ReservationService.Tests.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    private static async Task<(int StatusCode, string Body)> InvokeAsync(RequestDelegate next)
    {
        var middleware = new ExceptionHandlingMiddleware(next, NullLogger<ExceptionHandlingMiddleware>.Instance);
        var context = new DefaultHttpContext { Response = { Body = new MemoryStream() } };

        await middleware.InvokeAsync(context);

        context.Response.Body.Seek(0, SeekOrigin.Begin);
        return (context.Response.StatusCode, await new StreamReader(context.Response.Body).ReadToEndAsync());
    }

    [Fact]
    public async Task InvokeAsync_MapsReservationLimitExceeded_To400WithCurrentReservations()
    {
        var (statusCode, body) = await InvokeAsync(_ => throw new ReservationLimitExceededException("limit", 5));

        Assert.Equal(400, statusCode);
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("RESERVATION_LIMIT_EXCEEDED", doc.RootElement.GetProperty("error").GetString());
        Assert.Equal(5, doc.RootElement.GetProperty("currentReservations").GetInt32());
    }

    [Fact]
    public async Task InvokeAsync_MapsUnknownException_To500()
    {
        var (statusCode, body) = await InvokeAsync(_ => throw new InvalidOperationException("boom"));

        Assert.Equal(500, statusCode);
        using var doc = JsonDocument.Parse(body);
        Assert.Equal("INTERNAL_SERVER_ERROR", doc.RootElement.GetProperty("error").GetString());
    }

    [Fact]
    public async Task InvokeAsync_PassesThrough_WhenNoExceptionThrown()
    {
        var (statusCode, _) = await InvokeAsync(context => { context.Response.StatusCode = 200; return Task.CompletedTask; });
        Assert.Equal(200, statusCode);
    }
}