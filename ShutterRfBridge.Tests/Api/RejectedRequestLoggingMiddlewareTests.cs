using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Testing;
using ShutterRfBridge.Api;
using ShutterRfBridge.Tests.TestSupport;

namespace ShutterRfBridge.Tests.Api;

public sealed class RejectedRequestLoggingMiddlewareTests
{
    private readonly FakeLogger<RejectedRequestLoggingMiddleware> _logger = new();

    [Theory]
    [InlineData(StatusCodes.Status400BadRequest)]
    [InlineData(StatusCodes.Status404NotFound)]
    [InlineData(StatusCodes.Status405MethodNotAllowed)]
    public async Task InvokeAsync_ClientError_LogsAWarning(int statusCode)
    {
        var middleware = new RejectedRequestLoggingMiddleware(Answer(statusCode), _logger);

        await middleware.InvokeAsync(new DefaultHttpContext());

        Assert.Equal(LogLevel.Warning, Assert.Single(_logger.Collector.GetSnapshot()).Level);
    }

    [Fact]
    public async Task InvokeAsync_Accepted_LogsNothing()
    {
        var middleware = new RejectedRequestLoggingMiddleware(Answer(StatusCodes.Status202Accepted), _logger);

        await middleware.InvokeAsync(new DefaultHttpContext());

        Assert.Empty(_logger.Collector.GetSnapshot());
    }

    [Fact]
    public async Task InvokeAsync_UnknownPathSentToTheService_LogsAWarning()
    {
        await using var application = new BridgeApplication();
        application.ReplaceService<ILogger<RejectedRequestLoggingMiddleware>>(_logger);

        await application.PostAsync("/shutters/3/1/20000/1000/0");

        Assert.Equal(LogLevel.Warning, Assert.Single(_logger.Collector.GetSnapshot()).Level);
    }

    private static RequestDelegate Answer(int statusCode) => context =>
    {
        context.Response.StatusCode = statusCode;
        return Task.CompletedTask;
    };
}
