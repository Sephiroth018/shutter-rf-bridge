using System.Globalization;

namespace ShutterRfBridge.Weather;

/// <summary>
///     The location for the rain, from the <see cref="SettingKey" /> setting: its coordinates, or why they can't be used.
///     Read once, so that a missing or broken setting never stops the shutters.
/// </summary>
public abstract record Location
{
    public const string SettingKey = "Weather:Location";

    internal static Location Read(IConfiguration configuration)
    {
        Coordinates? coordinates;
        try
        {
            coordinates = configuration.GetSection(SettingKey).Get<Coordinates>();
        }
        // The binder fails on a missing, empty or non-numeric coordinate, naming it.
        catch (InvalidOperationException exception)
        {
            return new InvalidLocation(exception.Message);
        }

        if (coordinates is null)
        {
            return new InvalidLocation($"{SettingKey} missing");
        }

        List<string> problems = [];
        AddRangeProblem(problems, nameof(Coordinates.Latitude), coordinates.Latitude, Coordinates.MaxLatitude);
        AddRangeProblem(problems, nameof(Coordinates.Longitude), coordinates.Longitude, Coordinates.MaxLongitude);
        return problems.Count == 0 ? new ValidLocation(coordinates) : new InvalidLocation(string.Join(", ", problems));
    }

    private static void AddRangeProblem(List<string> problems, string coordinateName, double degrees, double max)
    {
        if (!double.IsFinite(degrees) || Math.Abs(degrees) > max)
        {
            problems.Add(string.Create(CultureInfo.InvariantCulture,
                $"{ConfigurationPath.Combine(SettingKey, coordinateName)} {degrees} out of range"));
        }
    }
}
