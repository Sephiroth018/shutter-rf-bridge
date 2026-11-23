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
    /// <summary>The run with its one step, which is missing when the response has no timestamp or no value.</summary>
    /// <exception cref="JsonException">The response isn't one point with at most one step.</exception>
    public Nowcast ToNowcast()
    {
        if (Features is not [var feature])
        {
            throw new JsonException($"The nowcast response has {Features.Count} features instead of one.");
        }

        var amounts = feature.Properties.Parameters.Rr.Data;
        if (amounts.Count != Timestamps.Count || Timestamps.Count > 1)
        {
            throw new JsonException(
                $"The nowcast response has {amounts.Count} values for {Timestamps.Count} timestamps instead of at most one step.");
        }

        var step = (Timestamps, amounts) is ([var time], [{ } millimetres]) ? new NowcastStep(time, millimetres) : null;
        return new Nowcast(ReferenceTime, step);
    }

    internal sealed record Feature(FeatureProperties Properties);

    internal sealed record FeatureProperties(ParameterSet Parameters);

    internal sealed record ParameterSet(Parameter Rr);

    internal sealed record Parameter(IReadOnlyList<double?> Data);
}
