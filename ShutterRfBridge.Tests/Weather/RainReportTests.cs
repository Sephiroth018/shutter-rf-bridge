using ShutterRfBridge.Weather;

namespace ShutterRfBridge.Tests.Weather;

public sealed class RainReportTests
{
    private static readonly DateTimeOffset ReferenceTime = new(2026, 11, 23, 18, 15, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset StepTime = new(2026, 11, 23, 18, 30, 0, TimeSpan.Zero);
    private static readonly TimeSpan StalenessLimit = TimeSpan.FromHours(1);

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.3)]
    public void From_CurrentStep_ReturnsItsAmount(double millimetres)
    {
        var nowcast = new Nowcast(ReferenceTime, [new NowcastStep(StepTime, millimetres)]);

        var report = RainReport.From(nowcast, StepTime, StalenessLimit);

        Assert.Equal(new RainAmount(millimetres), report);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(14)]
    [InlineData(14.999)]
    public void From_TimeWithinTheStep_ReturnsThatStepsAmount(double minutesIntoStep)
    {
        var nowcast = new Nowcast(ReferenceTime,
            [new NowcastStep(StepTime.AddMinutes(-15), 0.1), new NowcastStep(StepTime, 0.3)]);

        var report = RainReport.From(nowcast, StepTime.AddMinutes(minutesIntoStep), StalenessLimit);

        Assert.Equal(new RainAmount(0.3), report);
    }

    [Fact]
    public void From_RunExactlyAtTheLimit_ReturnsTheAmount()
    {
        var nowcast = new Nowcast(ReferenceTime, [new NowcastStep(StepTime.AddMinutes(45), 0.3)]);

        var report = RainReport.From(nowcast, ReferenceTime + StalenessLimit, StalenessLimit);

        Assert.Equal(new RainAmount(0.3), report);
    }

    [Fact]
    public void From_RunOlderThanTheLimit_ReturnsUnknownNamingTheRun()
    {
        var nowcast = new Nowcast(ReferenceTime, [new NowcastStep(StepTime.AddMinutes(45), 0.3)]);

        var report = RainReport.From(nowcast, ReferenceTime + StalenessLimit + TimeSpan.FromMinutes(1),
            StalenessLimit);

        Assert.Equal(new RainUnknown("the latest nowcast run, from 2026-11-23 18:15:00Z, is older than 01:00:00"),
            report);
    }

    [Fact]
    public void From_NoStepForNow_ReturnsUnknownNamingTheStep()
    {
        var nowcast = new Nowcast(ReferenceTime, []);

        var report = RainReport.From(nowcast, StepTime.AddMinutes(5), StalenessLimit);

        Assert.Equal(new RainUnknown("the nowcast has no value for 2026-11-23 18:30:00Z"), report);
    }

    [Theory]
    [InlineData(0.0, "0")]
    [InlineData(0.3, "0.3")]
    [InlineData(12.25, "12.25")]
    public void ToAnswer_Amount_ReturnsItWithADecimalPoint(double millimetres, string answer)
    {
        Assert.Equal(answer, new RainAmount(millimetres).ToAnswer());
    }

    [Fact]
    public void ToAnswer_Unknown_ReturnsUnknown()
    {
        Assert.Equal("unknown", new RainUnknown("any reason").ToAnswer());
    }
}
