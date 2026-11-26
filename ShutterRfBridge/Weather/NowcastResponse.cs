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
    ///     The run with its values for the <paramref name="stepTimes" /> it covers; one ending early covers the first
    ///     few.
    /// </summary>
    /// <exception cref="JsonException">The response isn't one point with the steps asked for.</exception>
    public Nowcast ToNowcast(IReadOnlyList<DateTimeOffset> stepTimes)
    {
        // Not null: RespectNullableAnnotations doesn't reach a list's elements.
        if (Features is not [{ } feature])
        {
            throw new JsonException($"The nowcast response has {Features.Count} features instead of one point.");
        }

        var amounts = feature.Properties.Parameters.Rr.Data;
        if (amounts.Count != Timestamps.Count)
        {
            throw new JsonException(
                $"The nowcast response has {amounts.Count} values for {Timestamps.Count} timestamps.");
        }

        if (Timestamps.Count > stepTimes.Count || !Timestamps.SequenceEqual(stepTimes.Take(Timestamps.Count)))
        {
            throw new JsonException(
                $"The nowcast response's steps at {string.Join(", ", Timestamps.Select(time => $"{time:u}"))} aren't the ones asked for, from {stepTimes[0]:u} on.");
        }

        return new Nowcast(ReferenceTime, amounts);
    }

    internal sealed record Feature(FeatureProperties Properties);

    internal sealed record FeatureProperties(ParameterSet Parameters);

    internal sealed record ParameterSet(Parameter Rr);

    internal sealed record Parameter(IReadOnlyList<double?> Data);
}
