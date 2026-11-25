using ShutterRfBridge.Weather;

namespace ShutterRfBridge.Tests.Weather;

public sealed class LocationTests
{
    [Fact]
    public void Read_BothCoordinates_ReturnsThem()
    {
        var location = Read("47.5", "-13.25");

        Assert.Equal(new ValidLocation(new Coordinates(47.5, -13.25)), location);
    }

    [Fact]
    public void Read_NoLocation_ReturnsItAsMissing()
    {
        var location = Location.Read(new WeatherOptions());

        Assert.Equal(new InvalidLocation("Weather:Location is missing."), location);
    }

    [Theory]
    [InlineData(null, "13.5", "Weather:Location:Latitude is missing or empty.")]
    [InlineData("47.5", " ", "Weather:Location:Longitude is missing or empty.")]
    [InlineData("47,5", "13.5", "Weather:Location:Latitude \"47,5\" is not a number.")]
    [InlineData("91", "13.5", "Weather:Location:Latitude 91 is outside -90 to 90.")]
    [InlineData("-91", "13.5", "Weather:Location:Latitude -91 is outside -90 to 90.")]
    [InlineData("47.5", "180.5", "Weather:Location:Longitude 180.5 is outside -180 to 180.")]
    [InlineData("47.5", "-181", "Weather:Location:Longitude -181 is outside -180 to 180.")]
    [InlineData("NaN", "13.5", "Weather:Location:Latitude NaN is outside -90 to 90.")]
    [InlineData("east", "",
        "Weather:Location:Latitude \"east\" is not a number. Weather:Location:Longitude is missing or empty.")]
    public void Read_BrokenCoordinate_ReturnsTheProblemNamingIt(string? latitude, string? longitude, string problem)
    {
        var location = Read(latitude, longitude);

        Assert.Equal(new InvalidLocation(problem), location);
    }

    private static Location Read(string? latitude, string? longitude) =>
        Location.Read(new WeatherOptions
        {
            Location = new LocationSettings { Latitude = latitude, Longitude = longitude }
        });
}
