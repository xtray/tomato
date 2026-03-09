namespace Tomato.WindowsCore;

public static class WindowsTaskDirectFocusGate
{
    public static bool AllowsDirectFocus(PomodoroSnapshot snapshot, bool hasSessionTask)
    {
        return !snapshot.IsRunning &&
               snapshot.Phase == PomodoroPhase.Idle &&
               !hasSessionTask;
    }
}
