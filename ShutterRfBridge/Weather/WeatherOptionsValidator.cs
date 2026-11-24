using Microsoft.Extensions.Options;

namespace ShutterRfBridge.Weather;

/// <summary>
///     Fails the startup when <see cref="WeatherOptions" /> are invalid. Leaving the location out is valid: the rain is a
///     bonus, so the shutters work without it.
/// </summary>
internal sealed class WeatherOptionsValidator : IValidateOptions<WeatherOptions>
{
    private const string LocationKey = $"{WeatherOptions.SectionName}:{nameof(WeatherOptions.Location)}";

    public ValidateOptionsResult Validate(string? name, WeatherOptions options)
    {
        List<string> failures = [];
        if (options.Location is { } location)
        {
            AddCoordinateFailure(failures, nameof(Coordinates.Latitude), location.Latitude, Coordinates.MaxLatitude);
            AddCoordinateFailure(failures, nameof(Coordinates.Longitude), location.Longitude,
                Coordinates.MaxLongitude);
        }

        if (options.RequestTimeout <= TimeSpan.Zero)
        {
            failures.Add($"{WeatherOptions.SectionName}:{nameof(WeatherOptions.RequestTimeout)} must be positive.");
        }

        return failures.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(failures);
    }

    private static void AddCoordinateFailure(List<string> failures, string coordinateName, double degrees, double max)
    {
        if (!double.IsFinite(degrees) || Math.Abs(degrees) > max)
        {
            failures.Add($"{LocationKey}:{coordinateName} {degrees} is outside -{max} to {max}.");
        }
    }
}
