using System.Text.Json;

namespace ShutterRfBridge.Weather;

/// <summary>Reports the rain at the configured coordinates, now and shortly ahead, fetching the nowcast for every report.</summary>
public class RainReporter
{
    // The current step and the next two: the nowcast lags about 30 minutes, and shutters closing early cost less than
    // shutters closing once the rain has begun.
    private const int StepCount = 3;

    // A run comes every 15 minutes, so an older one means several runs are missing.
    private static readonly TimeSpan StalenessLimit = TimeSpan.FromHours(1);

    private readonly NowcastClient _client;
    private readonly RainReportLog _log;
    private readonly TimeProvider _timeProvider;

    public RainReporter(NowcastClient client, TimeProvider timeProvider, RainReportLog log)
    {
        _client = client;
        _timeProvider = timeProvider;
        _log = log;
    }

    /// <summary>
    ///     The millimetres of rain at <paramref name="location" /> in the heaviest of the next 15-minute steps, the current
    ///     one included, or <see langword="null" /> when none of them has a value; the log gets the reason.
    /// </summary>
    public async Task<double?> ReportAsync(Coordinates location, CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var stepTime = Nowcast.StepTimeAt(now);
        Nowcast nowcast;
        try
        {
            nowcast = await _client.GetStepsAsync(location, stepTime, StepCount, cancellationToken);
        }
        catch (HttpRequestException exception)
        {
            return ReportUnknown($"request failed: {exception.Message}");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ReportUnknown($"timed out after {NowcastClient.RequestTimeout.TotalSeconds} s");
        }
        catch (JsonException exception)
        {
            return ReportUnknown($"unreadable response: {exception.Message}");
        }

        if (now - nowcast.ReferenceTime > StalenessLimit)
        {
            return ReportUnknown($"stale run from {nowcast.ReferenceTime:u}");
        }

        // Max leaves out steps without a value, and is null when none has one.
        if (nowcast.Millimetres.Max() is not { } heaviest)
        {
            return ReportUnknown($"no values from {stepTime:u}");
        }

        _log.RecordAmount(heaviest);
        return heaviest;
    }

    private double? ReportUnknown(string reason)
    {
        _log.RecordUnknown(reason);
        return null;
    }
}
