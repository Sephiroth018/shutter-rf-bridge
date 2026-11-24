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
        List<string?> failures = [];
        if (options.Location is { } location && (location.Latitude is not null || location.Longitude is not null))
        {
            failures.Add(ValidateCoordinate(nameof(Coordinates.Latitude), location.Latitude, Coordinates.MaxLatitude));
            failures.Add(ValidateCoordinate(nameof(Coordinates.Longitude), location.Longitude,
                Coordinates.MaxLongitude));
        }

        failures.Add(ValidateRequestTimeout(options.RequestTimeout));
        var messages = failures.OfType<string>().ToList();
        return messages.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(messages);
    }

    private static string? ValidateCoordinate(string coordinateName, double? coordinate, double max) =>
        coordinate switch
        {
            null => $"{LocationKey}:{coordinateName} is missing.",
            { } degrees when !double.IsFinite(degrees) || Math.Abs(degrees) > max =>
                $"{LocationKey}:{coordinateName} {degrees} is outside -{max} to {max}.",
            _ => null
        };

    private static string? ValidateRequestTimeout(TimeSpan timeout) =>
        timeout <= TimeSpan.Zero
            ? $"{WeatherOptions.SectionName}:{nameof(WeatherOptions.RequestTimeout)} must be positive."
            : null;
}
