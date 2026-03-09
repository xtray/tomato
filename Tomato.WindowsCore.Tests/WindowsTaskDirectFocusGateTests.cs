using Tomato.WindowsCore;
using Xunit;

namespace Tomato.WindowsCore.Tests;

public class WindowsTaskDirectFocusGateTests
{
    [Fact]
    public void AllowsDirectFocus_WhenEngineIsIdleAndNoSessionTaskExists()
    {
        var snapshot = new PomodoroSnapshot(
            PomodoroPhase.Idle,
            RemainingSeconds: 0,
            PhaseTotalSeconds: 0,
            CompletedWorkSessions: 0,
            IsRunning: false
        );

        var allowed = WindowsTaskDirectFocusGate.AllowsDirectFocus(snapshot, hasSessionTask: false);

        Assert.True(allowed);
    }

    [Fact]
    public void BlocksDirectFocus_WhenSessionIsPausedButResumable()
    {
        var snapshot = new PomodoroSnapshot(
            PomodoroPhase.Work,
            RemainingSeconds: 1200,
            PhaseTotalSeconds: 1500,
            CompletedWorkSessions: 0,
            IsRunning: false
        );

        var allowed = WindowsTaskDirectFocusGate.AllowsDirectFocus(snapshot, hasSessionTask: true);

        Assert.False(allowed);
    }

    [Fact]
    public void BlocksDirectFocus_WhenSessionIsRunning()
    {
        var snapshot = new PomodoroSnapshot(
            PomodoroPhase.Work,
            RemainingSeconds: 1200,
            PhaseTotalSeconds: 1500,
            CompletedWorkSessions: 0,
            IsRunning: true
        );

        var allowed = WindowsTaskDirectFocusGate.AllowsDirectFocus(snapshot, hasSessionTask: true);

        Assert.False(allowed);
    }
}
