using System.Globalization;

namespace ShutterRfBridge.Weather;

/// <summary>
///     The house's location from the settings: its coordinates, or why they can't be used. Read once, so that a missing or
///     broken location never stops the shutters.
/// </summary>
public abstract record HouseLocation
{
    private const string LocationKey = $"{WeatherOptions.SectionName}:{nameof(WeatherOptions.Location)}";

    internal static HouseLocation Read(WeatherOptions options)
    {
        if (options.Location is not { } settings)
        {
            return new UnusableHouseLocation($"{LocationKey} is missing.");
        }

        List<string> problems = [];
        var latitude = ReadCoordinate(problems, nameof(Coordinates.Latitude), settings.Latitude,
            Coordinates.MaxLatitude);
        var longitude = ReadCoordinate(problems, nameof(Coordinates.Longitude), settings.Longitude,
            Coordinates.MaxLongitude);
        return latitude is { } validLatitude && longitude is { } validLongitude
            ? new UsableHouseLocation(new Coordinates(validLatitude, validLongitude))
            : new UnusableHouseLocation(string.Join(" ", problems));
    }

    /// <summary>The coordinate's degrees, or <see langword="null" /> after adding its problem to <paramref name="problems" />.</summary>
    private static double? ReadCoordinate(List<string> problems, string coordinateName, string? text, double max)
    {
        var key = $"{LocationKey}:{coordinateName}";
        if (string.IsNullOrWhiteSpace(text))
        {
            problems.Add($"{key} is missing or empty.");
            return null;
        }

        if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var degrees))
        {
            problems.Add($"{key} \"{text}\" is not a number.");
            return null;
        }

        if (!double.IsFinite(degrees) || Math.Abs(degrees) > max)
        {
            problems.Add(string.Create(CultureInfo.InvariantCulture, $"{key} {degrees} is outside -{max} to {max}."));
            return null;
        }

        return degrees;
    }
}
