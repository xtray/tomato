using Tomato.WindowsCore;
using Xunit;

namespace Tomato.WindowsCore.Tests;

public class WindowsTaskDirectFocusGateTests
{
    [Fact]
    public void ResolveDoubleClickAction_StartsFocus_WhenIdleAndNoSessionTaskExists()
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

        Assert.Equal(WindowsTaskDoubleClickAction.StartFocus, action);
    }

    [Fact]
    public void ResolveDoubleClickAction_ReopensFloatingWindow_WhenRunningSessionMatchesClickedTask()
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
    public void ResolveDoubleClickAction_ReopensFloatingWindow_WhenPausedSessionMatchesClickedTask()
    {
        var snapshot = new PomodoroSnapshot(
            PomodoroPhase.Work,
            RemainingSeconds: 1200,
            PhaseTotalSeconds: 1500,
            CompletedWorkSessions: 0,
            IsRunning: false
        );

        var action = WindowsTaskDirectFocusGate.ResolveDoubleClickAction(
            snapshot,
            hasSessionTask: true,
            clickedTaskIsSessionTask: true
        );

        Assert.Equal(WindowsTaskDoubleClickAction.ReopenFloatingWindow, action);
    }

    [Fact]
    public void ResolveDoubleClickAction_StartsFocus_WhenResetLeavesWorkReadyWithoutSessionTask()
    {
        var snapshot = new PomodoroSnapshot(
            PomodoroPhase.Work,
            RemainingSeconds: 1500,
            PhaseTotalSeconds: 1500,
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
    public void ResolveDoubleClickAction_DoesNothing_WhenRunningSessionBelongsToDifferentTask()
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

        Assert.Equal(WindowsTaskDoubleClickAction.None, action);
    }

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
    public void AllowsDirectFocus_WhenResetLeavesWorkReadyWithoutSessionTask()
    {
        var snapshot = new PomodoroSnapshot(
            PomodoroPhase.Work,
            RemainingSeconds: 1500,
            PhaseTotalSeconds: 1500,
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
