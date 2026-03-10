namespace Tomato.WindowsCore;

public enum WindowsTimerCompletionAudioEvent
{
    None,
    WorkCompleted,
    BreakCompleted
}

public static class WindowsTimerCompletionAudioEventResolver
{
    public static WindowsTimerCompletionAudioEvent Resolve(PomodoroSnapshot before, PomodoroSnapshot after)
    {
        if (before.Phase == PomodoroPhase.Work &&
            after.Phase is PomodoroPhase.ShortBreak or PomodoroPhase.LongBreak)
        {
            return WindowsTimerCompletionAudioEvent.WorkCompleted;
        }

        if (before.Phase is PomodoroPhase.ShortBreak or PomodoroPhase.LongBreak &&
            after.Phase == PomodoroPhase.Work &&
            before.IsRunning &&
            !after.IsRunning)
        {
            return WindowsTimerCompletionAudioEvent.BreakCompleted;
        }

        return WindowsTimerCompletionAudioEvent.None;
    }
}
