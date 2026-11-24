using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using ShutterRfBridge.Weather;

namespace ShutterRfBridge.Api;

/// <summary>
///     The endpoint the CCU3 polls for the rain: always <c>200 OK</c> with plain text, the millimetres or
///     <c>unknown</c>.
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
    public async Task<ContentResult> Rain(CancellationToken cancellationToken)
    {
        var millimetres = await _reporter.ReportAsync(cancellationToken);
        // HM script's ToFloat reads a decimal point, whatever the Pi's culture.
        return Content(millimetres?.ToString(CultureInfo.InvariantCulture) ?? "unknown");
    }
}
