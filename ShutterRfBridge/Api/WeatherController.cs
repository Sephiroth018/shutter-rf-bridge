using System.Diagnostics;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using ShutterRfBridge.Weather;

namespace ShutterRfBridge.Api;

/// <summary>
///     The endpoint the CCU3 polls for the rain, answering plain text: <c>200 OK</c> with the millimetres or
///     <c>unknown</c>, or <c>500</c> with the problem while the location is invalid.
/// </summary>
[ApiController]
[Route("weather")]
public class WeatherController : ControllerBase
{
    private readonly Location _location;
    private readonly RainReportLog _log;
    private readonly RainReporter _reporter;

    public WeatherController(Location location, RainReporter reporter, RainReportLog log)
    {
        _location = location;
        _reporter = reporter;
        _log = log;
    }

    [HttpGet("rain")]
    public async Task<ContentResult> Rain(CancellationToken cancellationToken) =>
        _location switch
        {
            ValidLocation valid => await ReportRainAsync(valid.Coordinates, cancellationToken),
            InvalidLocation invalid => ReportInvalidLocation(invalid.Problem),
            _ => throw new UnreachableException()
        };

    private async Task<ContentResult> ReportRainAsync(Coordinates location, CancellationToken cancellationToken)
    {
        var millimetres = await _reporter.ReportAsync(location, cancellationToken);
        // HM script's ToFloat reads a decimal point, whatever the Pi's culture.
        return Content(millimetres?.ToString(CultureInfo.InvariantCulture) ?? "unknown");
    }

    private ContentResult ReportInvalidLocation(string problem)
    {
        // Every request, unlike the one error at startup: the problem needs fixing.
        _log.RecordInvalidLocation(LogLevel.Warning, problem);
        var answer = Content(problem);
        answer.StatusCode = StatusCodes.Status500InternalServerError;
        return answer;
    }
}
