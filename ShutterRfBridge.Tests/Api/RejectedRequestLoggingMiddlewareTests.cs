using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ShutterRfBridge.Api;
using ShutterRfBridge.Tests.TestSupport;

namespace ShutterRfBridge.Tests.Api;

public sealed class RejectedRequestLoggingMiddlewareTests
{
    private readonly ILogger<RejectedRequestLoggingMiddleware> _logger =
        Substitute.For<ILogger<RejectedRequestLoggingMiddleware>>();

    public RejectedRequestLoggingMiddlewareTests()
    {
        _logger.IsEnabled(Arg.Any<LogLevel>()).Returns(true);
    }

    [Theory]
    [InlineData(StatusCodes.Status400BadRequest)]
    [InlineData(StatusCodes.Status404NotFound)]
    [InlineData(StatusCodes.Status405MethodNotAllowed)]
    public async Task InvokeAsync_ClientError_LogsAWarning(int statusCode)
    {
        var middleware = new RejectedRequestLoggingMiddleware(Answer(statusCode), _logger);

        await middleware.InvokeAsync(new DefaultHttpContext());

        _logger.Received(1).Log(LogLevel.Warning, Arg.Any<EventId>(), Arg.Any<Arg.AnyType>(), null,
            Arg.Any<Func<Arg.AnyType, Exception?, string>>());
    }

    [Fact]
    public async Task InvokeAsync_Accepted_LogsNothing()
    {
        var middleware = new RejectedRequestLoggingMiddleware(Answer(StatusCodes.Status202Accepted), _logger);

        await middleware.InvokeAsync(new DefaultHttpContext());

        _logger.DidNotReceive().Log(LogLevel.Warning, Arg.Any<EventId>(), Arg.Any<Arg.AnyType>(),
            Arg.Any<Exception?>(), Arg.Any<Func<Arg.AnyType, Exception?, string>>());
    }

    [Fact]
    public async Task InvokeAsync_UnknownPathSentToTheService_LogsAWarning()
    {
        await using var application = new BridgeApplication();
        application.ReplaceService(_logger);

        await application.PostAsync("/shutters/3/1/20000/1000/0");

        _logger.Received(1).Log(LogLevel.Warning, Arg.Any<EventId>(), Arg.Any<Arg.AnyType>(), null,
            Arg.Any<Func<Arg.AnyType, Exception?, string>>());
    }

    private static RequestDelegate Answer(int statusCode) => context =>
    {
        context.Response.StatusCode = statusCode;
        return Task.CompletedTask;
    };
}
