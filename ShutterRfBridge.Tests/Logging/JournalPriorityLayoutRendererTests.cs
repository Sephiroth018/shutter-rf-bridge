using NLog;
using ShutterRfBridge.Logging;

namespace ShutterRfBridge.Tests.Logging;

public sealed class JournalPriorityLayoutRendererTests
{
    public static TheoryData<LogLevel, string> JournalPriorities => new()
    {
        { LogLevel.Fatal, "<2>" },
        { LogLevel.Error, "<3>" },
        { LogLevel.Warn, "<4>" },
        { LogLevel.Info, "<6>" },
        { LogLevel.Debug, "<7>" },
        { LogLevel.Trace, "<7>" }
    };

    [Theory]
    [MemberData(nameof(JournalPriorities))]
    public void Render_Level_WritesItsJournalPriority(LogLevel level, string expectedPrefix)
    {
        var renderer = new JournalPriorityLayoutRenderer();
        var logEvent = new LogEventInfo { Level = level };

        var prefix = renderer.Render(logEvent);

        Assert.Equal(expectedPrefix, prefix);
    }
}
