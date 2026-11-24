using ShutterRfBridge.Weather;

namespace ShutterRfBridge.Tests.Weather;

public sealed class WeatherOptionsValidatorTests
{
    private readonly WeatherOptionsValidator _validator = new();

    [Fact]
    public void Validate_ValidOptions_Succeeds()
    {
        var result = _validator.Validate(null, Options(new Coordinates { Latitude = 47.5, Longitude = 13.5 }));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_NoLocation_Succeeds()
    {
        var result = _validator.Validate(null, Options(null));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_BothCoordinatesEmpty_Succeeds()
    {
        var result = _validator.Validate(null, Options(new Coordinates()));

        Assert.True(result.Succeeded);
    }

    [Fact]
    public void Validate_OnlyLatitude_FailsNamingTheLongitude()
    {
        var result = _validator.Validate(null, Options(new Coordinates { Latitude = 47.5 }));

        Assert.Equal(["Weather:Location:Longitude is missing."], result.Failures);
    }

    [Theory]
    [InlineData(91, 13.5, "Weather:Location:Latitude 91 is outside -90 to 90.")]
    [InlineData(-91, 13.5, "Weather:Location:Latitude -91 is outside -90 to 90.")]
    [InlineData(47.5, 181, "Weather:Location:Longitude 181 is outside -180 to 180.")]
    [InlineData(47.5, -181, "Weather:Location:Longitude -181 is outside -180 to 180.")]
    [InlineData(double.NaN, 13.5, "Weather:Location:Latitude NaN is outside -90 to 90.")]
    public void Validate_CoordinateOutOfRange_FailsNamingIt(double latitude, double longitude, string failure)
    {
        var result = _validator.Validate(null, Options(new Coordinates { Latitude = latitude, Longitude = longitude }));

        Assert.Equal([failure], result.Failures);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_RequestTimeoutNotPositive_FailsNamingIt(int seconds)
    {
        var result = _validator.Validate(null, new WeatherOptions { RequestTimeout = TimeSpan.FromSeconds(seconds) });

        Assert.Equal(["Weather:RequestTimeout must be positive."], result.Failures);
    }

    private static WeatherOptions Options(Coordinates? location) => new()
    {
        Location = location, RequestTimeout = TimeSpan.FromSeconds(5)
    };
}
