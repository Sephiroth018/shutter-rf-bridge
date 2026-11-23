using System.Globalization;
using System.Text.Json;

namespace ShutterRfBridge.Weather;

/// <summary>Fetches GeoSphere Austria's rain nowcast (dataset <c>nowcast-v1-15min-1km</c>) for one point and one step.</summary>
public class NowcastClient
{
    // ISO 8601 in UTC, which the API takes for start and end.
    private const string StepTimeFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";
    public static readonly Uri BaseAddress = new("https://dataset.api.hub.geosphere.at/v1/");

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
    ///     Gets the latest run with its step at <paramref name="stepTime" />; the run has no step when it doesn't cover
    ///     that time.
    /// </summary>
    /// <exception cref="HttpRequestException">The request failed or didn't succeed.</exception>
    /// <exception cref="TaskCanceledException">The request timed out or was cancelled.</exception>
    /// <exception cref="JsonException">The response isn't a nowcast for one point.</exception>
    public async Task<Nowcast> GetStepAsync(
        double latitude,
        double longitude,
        DateTimeOffset stepTime,
        CancellationToken cancellationToken)
    {
        var time = stepTime.UtcDateTime.ToString(StepTimeFormat, CultureInfo.InvariantCulture);
        var path = string.Create(CultureInfo.InvariantCulture,
            $"timeseries/forecast/nowcast-v1-15min-1km?parameters=rr&lat_lon={latitude},{longitude}&start={time}&end={time}");
        var response = await _httpClient.GetFromJsonAsync<NowcastResponse>(path, JsonOptions, cancellationToken)
                       ?? throw new JsonException("The nowcast response is null.");
        return response.ToNowcast();
    }
}
