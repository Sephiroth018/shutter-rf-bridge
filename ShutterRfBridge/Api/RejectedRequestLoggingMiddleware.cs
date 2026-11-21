namespace ShutterRfBridge.Api;

/// <summary>
///     Logs every request answered with a client error, such as an unknown path, a wrong method or an invalid value,
///     since ASP.NET Core's own logging of them is filtered out.
/// </summary>
internal sealed partial class RejectedRequestLoggingMiddleware
{
    private readonly ILogger<RejectedRequestLoggingMiddleware> _logger;
    private readonly RequestDelegate _next;

    public RejectedRequestLoggingMiddleware(RequestDelegate next, ILogger<RejectedRequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);
        if (context.Response.StatusCode is >= 400 and < 500)
        {
            LogRejected(context.Request.Method, context.Request.Path, context.Response.StatusCode);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Rejected {Method} {Path} with {StatusCode}")]
    private partial void LogRejected(string method, PathString path, int statusCode);
}
