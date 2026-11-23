using Microsoft.Extensions.Options;

namespace ShutterRfBridge.Weather;

/// <summary>
///     Fails the startup when <see cref="WeatherOptions" /> are invalid. Leaving both coordinates out is valid: the rain
///     is a bonus, so the shutters work without it.
/// </summary>
internal sealed class WeatherOptionsValidator : IValidateOptions<WeatherOptions>
{
    private const string LocationKey = $"{WeatherOptions.SectionName}:{nameof(WeatherOptions.Location)}";

    public ValidateOptionsResult Validate(string? name, WeatherOptions options)
    {
        List<string> failures = [];
        if (options.Location is { } location && (location.Latitude is not null || location.Longitude is not null))
        {
            failures.AddRange(ValidateCoordinate(nameof(Coordinates.Latitude), location.Latitude,
                Coordinates.MaxLatitude));
            failures.AddRange(ValidateCoordinate(nameof(Coordinates.Longitude), location.Longitude,
                Coordinates.MaxLongitude));
        }

        failures.AddRange(ValidatePositive(nameof(WeatherOptions.StalenessLimit), options.StalenessLimit));
        failures.AddRange(ValidatePositive(nameof(WeatherOptions.RequestTimeout), options.RequestTimeout));
        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static IEnumerable<string> ValidateCoordinate(string coordinateName, double? coordinate, double max)
    {
        if (coordinate is not { } degrees)
        {
            yield return $"{LocationKey}:{coordinateName} is missing.";
        }
        else if (Math.Abs(degrees) > max)
        {
            yield return $"{LocationKey}:{coordinateName} {degrees} is outside -{max} to {max}.";
        }
    }

    private static IEnumerable<string> ValidatePositive(string limitName, TimeSpan limit)
    {
        if (limit <= TimeSpan.Zero)
        {
            yield return $"{WeatherOptions.SectionName}:{limitName} must be positive.";
        }
    }
}
