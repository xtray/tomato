namespace Tomato.WindowsCore;

public static class WindowsTimerStatusText
{
    public static string ResolveKey(PomodoroSnapshot snapshot, bool hasSessionTask)
    {
        return snapshot.Phase switch
        {
            PomodoroPhase.Work when snapshot.IsRunning => "timer.phase.work",
            PomodoroPhase.Work when hasSessionTask => "timer.status.paused",
            PomodoroPhase.ShortBreak => "timer.phase.short_break",
            PomodoroPhase.LongBreak => "timer.phase.long_break",
            _ => "timer.phase.ready"
        };
    }
}
