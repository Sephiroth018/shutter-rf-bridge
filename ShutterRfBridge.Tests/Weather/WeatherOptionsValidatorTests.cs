using ShutterRfBridge.Weather;

namespace ShutterRfBridge.Tests.Weather;

public sealed class WeatherOptionsValidatorTests
{
    private readonly WeatherOptionsValidator _validator = new();

    [Fact]
    public void Validate_ValidOptions_Succeeds()
    {
        var result = _validator.Validate(null, Options(new Coordinates(47.5, 13.5)));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_NoLocation_Succeeds()
    {
        var result = _validator.Validate(null, Options(null));

        Assert.True(result.Succeeded);
    }

    [Theory]
    [InlineData(91, 13.5, "Weather:Location:Latitude 91 is outside -90 to 90.")]
    [InlineData(-91, 13.5, "Weather:Location:Latitude -91 is outside -90 to 90.")]
    [InlineData(47.5, 181, "Weather:Location:Longitude 181 is outside -180 to 180.")]
    [InlineData(47.5, -181, "Weather:Location:Longitude -181 is outside -180 to 180.")]
    [InlineData(double.NaN, 13.5, "Weather:Location:Latitude NaN is outside -90 to 90.")]
    public void Validate_CoordinateOutOfRange_FailsNamingIt(double latitude, double longitude, string failure)
    {
        var result = _validator.Validate(null, Options(new Coordinates(latitude, longitude)));

        Assert.Equal([failure], result.Failures);
    }

    private static WeatherOptions Options(Coordinates? location) => new() { Location = location };
}
