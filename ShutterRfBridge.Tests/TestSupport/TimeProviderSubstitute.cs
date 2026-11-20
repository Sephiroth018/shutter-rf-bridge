using NSubstitute;

namespace ShutterRfBridge.Tests.TestSupport;

/// <summary>A substituted <see cref="System.TimeProvider" /> whose timers fire only when a test fires them.</summary>
internal sealed class TimeProviderSubstitute
{
    private readonly List<ScheduledTimer> _timers = [];

    public TimeProviderSubstitute()
    {
        TimeProvider.CreateTimer(Arg.Any<TimerCallback>(), Arg.Any<object?>(), Arg.Any<TimeSpan>(), Arg.Any<TimeSpan>())
            .Returns(call =>
            {
                var timer = new ScheduledTimer(call.ArgAt<TimerCallback>(0), call.ArgAt<object?>(1),
                    call.ArgAt<TimeSpan>(2), Substitute.For<ITimer>());
                _timers.Add(timer);
                return timer.Timer;
            });
    }

    public TimeProvider TimeProvider { get; } = Substitute.For<TimeProvider>();

    /// <summary>Every timer created so far, in order.</summary>
    public IReadOnlyList<ScheduledTimer> Timers => _timers;

    public sealed record ScheduledTimer(TimerCallback Callback, object? State, TimeSpan DueTime, ITimer Timer)
    {
        /// <summary>Runs the callback as if the due time had passed.</summary>
        public void Fire() => Callback(State);
    }
}
