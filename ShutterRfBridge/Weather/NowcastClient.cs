using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;

namespace ShutterRfBridge.Weather;

/// <summary>Fetches GeoSphere Austria's rain nowcast (dataset <c>nowcast-v1-15min-1km</c>) for one point and one step.</summary>
public class NowcastClient
{
    private const string DatasetAddress =
        "https://dataset.api.hub.geosphere.at/v1/timeseries/forecast/nowcast-v1-15min-1km";

    /// <summary>
    ///     How long the request may take: below the CCU3's <c>curl -m 10</c>, so the CCU3 gets <c>unknown</c> rather
    ///     than nothing.
    /// </summary>
    public static readonly TimeSpan RequestTimeout = TimeSpan.FromSeconds(5);

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
    ///     Gets the latest run with its values for <paramref name="stepCount" /> steps from
    ///     <paramref name="firstStepTime" /> on, as far as it covers them.
    /// </summary>
    /// <exception cref="HttpRequestException">The request failed or didn't succeed.</exception>
    /// <exception cref="IOException">The response broke off while its body was read.</exception>
    /// <exception cref="TaskCanceledException">The request timed out or was cancelled.</exception>
    /// <exception cref="JsonException">The response isn't a nowcast for one point and those steps.</exception>
    public async Task<Nowcast> GetStepsAsync(
        Coordinates location,
        DateTimeOffset firstStepTime,
        int stepCount,
        CancellationToken cancellationToken)
    {
        var stepTimes = Enumerable.Range(0, stepCount).Select(step => firstStepTime + step * Nowcast.StepLength)
            .ToList();
        var url = QueryHelpers.AddQueryString(DatasetAddress, new Dictionary<string, string?>
        {
            ["parameters"] = "rr",
            ["lat_lon"] = string.Create(CultureInfo.InvariantCulture, $"{location.Latitude},{location.Longitude}"),
            ["start"] = Format(stepTimes[0]),
            ["end"] = Format(stepTimes[^1])
        });
        var response = await _httpClient.GetFromJsonAsync<NowcastResponse>(url, JsonOptions, cancellationToken)
                       ?? throw new JsonException("The nowcast response is null.");
        return response.ToNowcast(stepTimes);
    }

    // ISO 8601 in UTC, which the API takes for start and end.
    private static string Format(DateTimeOffset stepTime) =>
        stepTime.UtcDateTime.ToString("o", CultureInfo.InvariantCulture);
}
