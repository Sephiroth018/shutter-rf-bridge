using System.Globalization;
using System.Text.Json;

namespace ShutterRfBridge.Weather;

/// <summary>Fetches GeoSphere Austria's rain nowcast (dataset <c>nowcast-v1-15min-1km</c>) for one point and one step.</summary>
public class NowcastClient
{
    private const string DatasetAddress =
        "https://dataset.api.hub.geosphere.at/v1/timeseries/forecast/nowcast-v1-15min-1km";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true
    };

    private readonly HttpClient _httpClient;

    public NowcastClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    ///     Gets the latest run with its value for the step at <paramref name="stepTime" />; the run has no value when it
    ///     doesn't cover that time.
    /// </summary>
    /// <exception cref="HttpRequestException">The request failed or didn't succeed.</exception>
    /// <exception cref="TaskCanceledException">The request timed out or was cancelled.</exception>
    /// <exception cref="JsonException">The response isn't a nowcast for one point and that step.</exception>
    public async Task<Nowcast> GetStepAsync(
        Coordinates location,
        DateTimeOffset stepTime,
        CancellationToken cancellationToken)
    {
        // ISO 8601 in UTC, which the API takes for start and end.
        var time = stepTime.UtcDateTime.ToString("o", CultureInfo.InvariantCulture);
        var url = string.Create(CultureInfo.InvariantCulture,
            $"{DatasetAddress}?parameters=rr&lat_lon={location.Latitude},{location.Longitude}&start={time}&end={time}");
        var response = await _httpClient.GetFromJsonAsync<NowcastResponse>(url, JsonOptions, cancellationToken)
                       ?? throw new JsonException("The nowcast response is null.");
        return response.ToNowcast(stepTime);
    }
}
