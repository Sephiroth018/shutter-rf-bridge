using System.Text;
using NLog;
using NLog.Config;
using NLog.LayoutRenderers;
using LogLevel = NLog.LogLevel;

namespace ShutterRfBridge.Logging;

/// <summary>
///     Writes the log event's level as the priority prefix that systemd reads from a service's console lines (the
///     <c>SD_*</c> prefixes of <c>sd-daemon(3)</c>), so the journal stores each line with its priority.
/// </summary>
[LayoutRenderer("journal-priority")]
[ThreadAgnostic]
internal sealed class JournalPriorityLayoutRenderer : LayoutRenderer
{
    protected override void Append(StringBuilder builder, LogEventInfo logEvent)
    {
        builder.Append(GetPrefix(logEvent.Level));
    }

    private static string GetPrefix(LogLevel level)
    {
        if (level >= LogLevel.Fatal)
        {
            return "<2>";
        }

        if (level >= LogLevel.Error)
        {
            return "<3>";
        }

        if (level >= LogLevel.Warn)
        {
            return "<4>";
        }

        return level >= LogLevel.Info ? "<6>" : "<7>";
    }
}
