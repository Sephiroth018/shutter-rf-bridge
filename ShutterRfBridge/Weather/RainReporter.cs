using System.Text.Json;
using Microsoft.Extensions.Options;

namespace ShutterRfBridge.Weather;

/// <summary>Reports the rain at the house now, fetching the nowcast for every report; any failure reports unknown.</summary>
internal sealed class RainReporter
{
    private readonly NowcastClient _client;
    private readonly RainReportLog _log;
    private readonly WeatherOptions _options;
    private readonly TimeProvider _timeProvider;

    public RainReporter(
        NowcastClient client,
        IOptions<WeatherOptions> options,
        TimeProvider timeProvider,
        RainReportLog log)
    {
        _client = client;
        _options = options.Value;
        _timeProvider = timeProvider;
        _log = log;
    }

    public async Task<RainReport> ReportAsync(CancellationToken cancellationToken)
    {
        var report = await GetReportAsync(cancellationToken);
        _log.Record(report);
        return report;
    }

    private async Task<RainReport> GetReportAsync(CancellationToken cancellationToken)
    {
        if (_options.Location is not { Latitude: { } latitude, Longitude: { } longitude })
        {
            return new RainUnknown("no location is configured");
        }

        var now = _timeProvider.GetUtcNow();
        try
        {
            var nowcast = await _client.GetStepAsync(latitude, longitude, Nowcast.StepTimeAt(now), cancellationToken);
            return RainReport.From(nowcast, now, _options.StalenessLimit);
        }
        catch (HttpRequestException exception)
        {
            return new RainUnknown($"the nowcast request failed: {exception.Message}");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return new RainUnknown($"the nowcast request took longer than {_options.RequestTimeout}");
        }
        catch (JsonException exception)
        {
            return new RainUnknown($"the nowcast response is unreadable: {exception.Message}");
        }
    }
}
