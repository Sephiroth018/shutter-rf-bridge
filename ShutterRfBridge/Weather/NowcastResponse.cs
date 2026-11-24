using System.Text.Json;

namespace ShutterRfBridge.Weather;

/// <summary>
///     GeoSphere's timeseries response, as far as the <c>rr</c> parameter needs it. Its member names follow the JSON's
///     keys through <see cref="JsonNamingPolicy.SnakeCaseLower" />.
/// </summary>
internal sealed record NowcastResponse(
    DateTimeOffset ReferenceTime,
    IReadOnlyList<DateTimeOffset> Timestamps,
    IReadOnlyList<NowcastResponse.Feature> Features)
{
    /// <summary>
    ///     The run with the heaviest rain among the <paramref name="stepTimes" /> it covers, which is missing when it has
    ///     no value for the first one. A run ending early covers only the first few.
    /// </summary>
    /// <exception cref="JsonException">The response isn't one point with the steps asked for.</exception>
    public Nowcast ToNowcast(IReadOnlyList<DateTimeOffset> stepTimes)
    {
        if (Features is not [var feature])
        {
            throw new JsonException($"The nowcast response has {Features.Count} features instead of one.");
        }

        var amounts = feature.Properties.Parameters.Rr.Data;
        if (amounts.Count != Timestamps.Count)
        {
            throw new JsonException(
                $"The nowcast response has {amounts.Count} values for {Timestamps.Count} timestamps.");
        }

        // Longer than the steps asked for, the timestamps can't equal their beginning.
        if (!Timestamps.SequenceEqual(stepTimes.Take(Timestamps.Count)))
        {
            throw new JsonException(
                $"The nowcast response's steps at {string.Join(", ", Timestamps.Select(time => $"{time:u}"))} aren't the ones asked for, from {stepTimes[0]:u} on.");
        }

        var millimetres = amounts is [not null, ..] ? amounts.Max() : null;
        return new Nowcast(ReferenceTime, millimetres);
    }

    internal sealed record Feature(FeatureProperties Properties);

    internal sealed record FeatureProperties(ParameterSet Parameters);

    internal sealed record ParameterSet(Parameter Rr);

    internal sealed record Parameter(IReadOnlyList<double?> Data);
}
