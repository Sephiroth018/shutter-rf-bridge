using System.Globalization;
using System.Text.Json;
using Flurl;
using Microsoft.Extensions.Options;

namespace ShutterRfBridge.Weather;

/// <summary>Fetches GeoSphere Austria's rain nowcast (dataset <c>nowcast-v1-15min-1km</c>) for one point and one step.</summary>
public class NowcastClient
{
    // ISO 8601 in UTC, which the API takes for start and end.
    private const string StepTimeFormat = "yyyy-MM-dd'T'HH:mm:ss'Z'";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        RespectNullableAnnotations = true,
        RespectRequiredConstructorParameters = true
    };

    private readonly Uri _apiAddress;
    private readonly HttpClient _httpClient;

    public NowcastClient(HttpClient httpClient, IOptions<WeatherOptions> options)
    {
        _httpClient = httpClient;
        _apiAddress = options.Value.ApiAddress
                      ?? throw new InvalidOperationException($"{nameof(WeatherOptions.ApiAddress)} isn't set.");
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
        var url = new Url(_apiAddress)
            .AppendPathSegments("timeseries", "forecast", "nowcast-v1-15min-1km")
            .SetQueryParam("parameters", "rr")
            .SetQueryParam("lat_lon", string.Create(CultureInfo.InvariantCulture, $"{latitude},{longitude}"))
            .SetQueryParam("start", time)
            .SetQueryParam("end", time);
        var response = await _httpClient.GetFromJsonAsync<NowcastResponse>(url.ToUri(), JsonOptions, cancellationToken)
                       ?? throw new JsonException("The nowcast response is null.");
        return response.ToNowcast();
    }
}
