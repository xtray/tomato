using Tomato.WindowsCore;
using Xunit;

namespace Tomato.WindowsCore.Tests;

public class WindowsTaskDoubleClickActionTests
{
    [Fact]
    public void Resolve_ReturnsStartDirectFocus_WhenIdleAndNoSessionTaskExists()
    {
        var snapshot = new PomodoroSnapshot(
            PomodoroPhase.Idle,
            RemainingSeconds: 0,
            PhaseTotalSeconds: 0,
            CompletedWorkSessions: 0,
            IsRunning: false
        );

        var action = WindowsTaskDirectFocusGate.ResolveDoubleClickAction(
            snapshot,
            hasSessionTask: false,
            clickedTaskIsSessionTask: false
        );

        Assert.Equal(WindowsTaskDoubleClickAction.StartDirectFocus, action);
    }

    [Fact]
    public void Resolve_ReturnsReopenFloatingWindow_WhenRunningSessionTaskIsDoubleClicked()
    {
        var snapshot = new PomodoroSnapshot(
            PomodoroPhase.Work,
            RemainingSeconds: 1200,
            PhaseTotalSeconds: 1500,
            CompletedWorkSessions: 0,
            IsRunning: true
        );

        var action = WindowsTaskDirectFocusGate.ResolveDoubleClickAction(
            snapshot,
            hasSessionTask: true,
            clickedTaskIsSessionTask: true
        );

        Assert.Equal(WindowsTaskDoubleClickAction.ReopenFloatingWindow, action);
    }

    [Fact]
    public void Resolve_ReturnsIgnore_WhenDifferentTaskIsDoubleClickedDuringRunningSession()
    {
        var snapshot = new PomodoroSnapshot(
            PomodoroPhase.Work,
            RemainingSeconds: 1200,
            PhaseTotalSeconds: 1500,
            CompletedWorkSessions: 0,
            IsRunning: true
        );

        var action = WindowsTaskDirectFocusGate.ResolveDoubleClickAction(
            snapshot,
            hasSessionTask: true,
            clickedTaskIsSessionTask: false
        );

        Assert.Equal(WindowsTaskDoubleClickAction.Ignore, action);
    }
}
