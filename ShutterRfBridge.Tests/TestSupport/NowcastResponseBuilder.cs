using System.Globalization;
using System.Text.Json.Nodes;

namespace ShutterRfBridge.Tests.TestSupport;

/// <summary>Builds the JSON of a GeoSphere nowcast response for one made-up point, in the API's own time format.</summary>
internal sealed class NowcastResponseBuilder
{
    public static readonly DateTimeOffset DefaultReferenceTime = new(2026, 11, 23, 18, 15, 0, TimeSpan.Zero);

    private readonly List<(DateTimeOffset Time, double? Millimetres)> _steps = [];
    private int _featureCount = 1;
    private DateTimeOffset _referenceTime = DefaultReferenceTime;

    public NowcastResponseBuilder WithReferenceTime(DateTimeOffset referenceTime)
    {
        _referenceTime = referenceTime;
        return this;
    }

    public NowcastResponseBuilder WithStep(DateTimeOffset time, double? millimetres)
    {
        _steps.Add((time, millimetres));
        return this;
    }

    public NowcastResponseBuilder WithFeatureCount(int featureCount)
    {
        _featureCount = featureCount;
        return this;
    }

    public string Build()
    {
        var response = new JsonObject
        {
            ["reference_time"] = Format(_referenceTime),
            ["media_type"] = "application/json",
            ["type"] = "FeatureCollection",
            ["version"] = "v1",
            ["timestamps"] = new JsonArray([.. _steps.Select(step => (JsonNode?)Format(step.Time))]),
            ["features"] = new JsonArray([.. Enumerable.Range(0, _featureCount).Select(_ => (JsonNode?)Feature())])
        };
        return response.ToJsonString();
    }

    private JsonObject Feature() => new()
    {
        ["type"] = "Feature",
        ["geometry"] = new JsonObject { ["type"] = "Point", ["coordinates"] = new JsonArray(13.5, 47.5) },
        ["properties"] = new JsonObject
        {
            ["parameters"] = new JsonObject
            {
                ["rr"] = new JsonObject
                {
                    ["name"] = "precipitation sum",
                    ["unit"] = "kg m-2",
                    ["data"] = new JsonArray([.. _steps.Select(step => (JsonNode?)step.Millimetres)])
                }
            }
        }
    };

    // GeoSphere writes times without seconds, e.g. 2026-11-23T18:15+00:00.
    private static string Format(DateTimeOffset time) =>
        time.ToString("yyyy-MM-ddTHH:mmzzz", CultureInfo.InvariantCulture);
}
