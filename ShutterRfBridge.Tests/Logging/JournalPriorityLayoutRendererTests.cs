using NLog;
using ShutterRfBridge.Logging;

namespace ShutterRfBridge.Tests.Logging;

public sealed class JournalPriorityLayoutRendererTests
{
    [Theory]
    [InlineData("Fatal", "<2>")]
    [InlineData("Error", "<3>")]
    [InlineData("Warn", "<4>")]
    [InlineData("Info", "<6>")]
    [InlineData("Debug", "<7>")]
    [InlineData("Trace", "<7>")]
    public void Render_Level_WritesItsJournalPriority(string level, string expectedPrefix)
    {
        var renderer = new JournalPriorityLayoutRenderer();
        var logEvent = new LogEventInfo { Level = LogLevel.FromString(level) };

        var prefix = renderer.Render(logEvent);

        Assert.Equal(expectedPrefix, prefix);
    }
}
