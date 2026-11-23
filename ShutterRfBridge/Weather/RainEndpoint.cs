using System.Net.Mime;

namespace ShutterRfBridge.Weather;

/// <summary>
///     The endpoint the CCU3 polls for the rain: always <c>200 OK</c> with <see cref="RainReport.ToAnswer" /> as plain
///     text. A minimal API rather than a controller, because a controller must be public and the weather types aren't.
/// </summary>
internal static class RainEndpoint
{
    public const string Path = "/weather/rain";

    public static IEndpointRouteBuilder MapRainEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet(Path, async (RainReporter reporter, CancellationToken cancellationToken) =>
            Results.Text((await reporter.ReportAsync(cancellationToken)).ToAnswer(), MediaTypeNames.Text.Plain));
        return endpoints;
    }
}
