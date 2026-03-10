using Tomato.WindowsCore;
using Xunit;

namespace Tomato.WindowsCore.Tests;

public sealed class WindowsTimerStatusTextTests
{
    [Fact]
    public void ResolveKey_ReturnsReadyWhenIdle()
    {
        var snapshot = new PomodoroSnapshot(PomodoroPhase.Idle, 0, 0, 0, false);

        Assert.Equal("timer.phase.ready", WindowsTimerStatusText.ResolveKey(snapshot, hasSessionTask: false));
    }

    [Fact]
    public void ResolveKey_ReturnsWorkWhenRunningWorkSession()
    {
        var snapshot = new PomodoroSnapshot(PomodoroPhase.Work, 1200, 1500, 0, true);

        Assert.Equal("timer.phase.work", WindowsTimerStatusText.ResolveKey(snapshot, hasSessionTask: true));
    }

    [Fact]
    public void ResolveKey_ReturnsPausedWhenWorkSessionCanResume()
    {
        var snapshot = new PomodoroSnapshot(PomodoroPhase.Work, 1200, 1500, 0, false);

        Assert.Equal("timer.status.paused", WindowsTimerStatusText.ResolveKey(snapshot, hasSessionTask: true));
    }

    [Fact]
    public void ResolveKey_ReturnsBreakPhaseKeys()
    {
        var shortBreak = new PomodoroSnapshot(PomodoroPhase.ShortBreak, 300, 300, 1, false);
        var longBreak = new PomodoroSnapshot(PomodoroPhase.LongBreak, 900, 900, 4, true);

        Assert.Equal("timer.phase.short_break", WindowsTimerStatusText.ResolveKey(shortBreak, hasSessionTask: true));
        Assert.Equal("timer.phase.long_break", WindowsTimerStatusText.ResolveKey(longBreak, hasSessionTask: true));
    }
}
