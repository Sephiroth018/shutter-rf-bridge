using Microsoft.Extensions.Configuration;
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
        var location = Location.Read(new ConfigurationBuilder().Build());

        Assert.Equal(new InvalidLocation("Weather:Location missing"), location);
    }

    [Theory]
    [InlineData("91", "13.5", "Weather:Location:Latitude 91 out of range")]
    [InlineData("-91", "13.5", "Weather:Location:Latitude -91 out of range")]
    [InlineData("47.5", "180.5", "Weather:Location:Longitude 180.5 out of range")]
    [InlineData("47.5", "-181", "Weather:Location:Longitude -181 out of range")]
    [InlineData("NaN", "13.5", "Weather:Location:Latitude NaN out of range")]
    [InlineData("91", "181", "Weather:Location:Latitude 91 out of range, Weather:Location:Longitude 181 out of range")]
    public void Read_CoordinateOutOfRange_ReturnsTheProblemNamingIt(string latitude, string longitude, string problem)
    {
        var location = Read(latitude, longitude);

        Assert.Equal(new InvalidLocation(problem), location);
    }

    [Theory]
    [InlineData(null, "13.5", "Latitude")]
    [InlineData("47.5", "", "Longitude")]
    [InlineData("47,5", "13.5", "Latitude")]
    [InlineData("47.5", "east", "Longitude")]
    public void Read_UnreadableCoordinate_ReturnsTheBindersProblemNamingIt(
        string? latitude,
        string longitude,
        string coordinateName)
    {
        var location = Read(latitude, longitude);

        Assert.Contains(coordinateName, Assert.IsType<InvalidLocation>(location).Problem, StringComparison.Ordinal);
    }

    private static Location Read(string? latitude, string longitude) =>
        Location.Read(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Weather:Location:Latitude"] = latitude, ["Weather:Location:Longitude"] = longitude
        }).Build());
}
