namespace Tomato.WindowsCore;

public enum WindowsTaskDoubleClickAction
{
    Ignore,
    None = Ignore,
    StartDirectFocus,
    StartFocus = StartDirectFocus,
    ReopenFloatingWindow,
    ResumeFloatingFocus
}

public static class WindowsTaskDoubleClickActionResolver
{
    public static WindowsTaskDoubleClickAction Resolve(
        PomodoroSnapshot snapshot,
        Guid? sessionTaskId,
        Guid clickedTaskId
    )
    {
        return WindowsTaskDirectFocusGate.ResolveDoubleClickActionCore(
            snapshot,
            hasSessionTask: sessionTaskId.HasValue,
            clickedTaskIsSessionTask: sessionTaskId == clickedTaskId
        );
    }
}

public static class WindowsTaskDirectFocusGate
{
    public static WindowsTaskDoubleClickAction ResolveDoubleClickAction(
        PomodoroSnapshot snapshot,
        bool hasSessionTask,
        bool clickedTaskIsSessionTask
    )
    {
        return ResolveDoubleClickActionCore(snapshot, hasSessionTask, clickedTaskIsSessionTask);
    }

    public static bool AllowsDirectFocus(PomodoroSnapshot snapshot, bool hasSessionTask)
    {
        return !snapshot.IsRunning &&
               snapshot.Phase is PomodoroPhase.Idle or PomodoroPhase.Work &&
               !hasSessionTask;
    }

    internal static WindowsTaskDoubleClickAction ResolveDoubleClickActionCore(
        PomodoroSnapshot snapshot,
        bool hasSessionTask,
        bool clickedTaskIsSessionTask
    )
    {
        if (AllowsDirectFocus(snapshot, hasSessionTask))
        {
            return WindowsTaskDoubleClickAction.StartDirectFocus;
        }

        if (hasSessionTask &&
            clickedTaskIsSessionTask &&
            snapshot.Phase != PomodoroPhase.Idle)
        {
            return snapshot.IsRunning
                ? WindowsTaskDoubleClickAction.ReopenFloatingWindow
                : WindowsTaskDoubleClickAction.ResumeFloatingFocus;
        }

        return WindowsTaskDoubleClickAction.Ignore;
    }
}
