using Microsoft.AspNetCore.Mvc;
using ShutterRfBridge.Weather;

namespace ShutterRfBridge.Api;

/// <summary>
///     The endpoint the CCU3 polls for the rain: always <c>200 OK</c> with <see cref="RainReport.ToAnswer" /> as plain
///     text.
/// </summary>
[ApiController]
[Route("weather")]
public class WeatherController : ControllerBase
{
    private readonly RainReporter _reporter;

    public WeatherController(RainReporter reporter)
    {
        _reporter = reporter;
    }

    [HttpGet("rain")]
    public async Task<ContentResult> Rain(CancellationToken cancellationToken) =>
        Content((await _reporter.ReportAsync(cancellationToken)).ToAnswer());
}
