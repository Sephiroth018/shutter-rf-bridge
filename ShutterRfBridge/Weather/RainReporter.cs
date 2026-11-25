using System.Text.Json;

namespace ShutterRfBridge.Weather;

/// <summary>Reports the rain at the house, now and shortly ahead, fetching the nowcast for every report.</summary>
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
    ///     one included, or <see langword="null" /> when the current step's are unknown; the log gets the reason.
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
            return ReportUnknown($"the nowcast request failed: {exception.Message}");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return ReportUnknown($"the nowcast request took longer than {NowcastClient.RequestTimeout}");
        }
        catch (JsonException exception)
        {
            return ReportUnknown($"the nowcast response is unreadable: {exception.Message}");
        }

        if (now - nowcast.ReferenceTime > StalenessLimit)
        {
            return ReportUnknown(
                $"the latest nowcast run, from {nowcast.ReferenceTime:u}, is older than {StalenessLimit}");
        }

        if (nowcast.Millimetres is not [not null, ..])
        {
            return ReportUnknown($"the nowcast has no value for {stepTime:u}");
        }

        // Later steps without a value are left out.
        var heaviest = nowcast.Millimetres.OfType<double>().Max();
        _log.RecordAmount(heaviest);
        return heaviest;
    }

    private double? ReportUnknown(string reason)
    {
        _log.RecordUnknown(reason);
        return null;
    }
}
