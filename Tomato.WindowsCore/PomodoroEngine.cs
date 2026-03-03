namespace Tomato.WindowsCore;

public enum PomodoroPhase
{
    Idle,
    Work,
    ShortBreak,
    LongBreak
}

public readonly record struct PomodoroSnapshot(
    PomodoroPhase Phase,
    int RemainingSeconds,
    int PhaseTotalSeconds,
    int CompletedWorkSessions,
    bool IsRunning
);

public sealed class PomodoroEngine
{
    private const int DefaultLongBreakInterval = 4;

    private int _workSeconds;
    private int _shortBreakSeconds;
    private int _longBreakSeconds;
    private int _remainingSeconds;
    private int _phaseTotalSeconds;
    private int _completedWorkSessions;
    private bool _isRunning;
    private PomodoroPhase _phase = PomodoroPhase.Idle;

    public PomodoroSnapshot Snapshot => new(
        _phase,
        _remainingSeconds,
        _phaseTotalSeconds,
        _completedWorkSessions,
        _isRunning
    );

    public void StartFocusSession(int workMinutes, int shortBreakMinutes, int longBreakMinutes)
    {
        if (workMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(workMinutes), "Work minutes must be greater than 0.");
        }

        if (shortBreakMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(shortBreakMinutes), "Short break minutes must be greater than 0.");
        }

        if (longBreakMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(longBreakMinutes), "Long break minutes must be greater than 0.");
        }

        if (!_isRunning &&
            _remainingSeconds > 0 &&
            _phase is PomodoroPhase.Work or PomodoroPhase.ShortBreak or PomodoroPhase.LongBreak)
        {
            _isRunning = true;
            return;
        }

        _workSeconds = workMinutes * 60;
        _shortBreakSeconds = shortBreakMinutes * 60;
        _longBreakSeconds = longBreakMinutes * 60;

        _phase = PomodoroPhase.Work;
        _remainingSeconds = _workSeconds;
        _phaseTotalSeconds = _workSeconds;
        _isRunning = true;
    }

    public void Pause()
    {
        if (_phase is PomodoroPhase.Work or PomodoroPhase.ShortBreak or PomodoroPhase.LongBreak)
        {
            _isRunning = false;
        }
    }

    public void Resume()
    {
        if (_phase is PomodoroPhase.Work or PomodoroPhase.ShortBreak or PomodoroPhase.LongBreak)
        {
            _isRunning = true;
        }
    }

    public void StopSession()
    {
        _isRunning = false;
    }

    public void ResetToWorkReady()
    {
        _isRunning = false;

        if (_workSeconds <= 0)
        {
            _phase = PomodoroPhase.Idle;
            _remainingSeconds = 0;
            _phaseTotalSeconds = 0;
            return;
        }

        _phase = PomodoroPhase.Work;
        _remainingSeconds = _workSeconds;
        _phaseTotalSeconds = _workSeconds;
    }

    public void Tick()
    {
        if (!_isRunning || _phase == PomodoroPhase.Idle)
        {
            return;
        }

        if (_remainingSeconds > 0)
        {
            _remainingSeconds--;
        }

        if (_remainingSeconds <= 0)
        {
            AdvanceToNextPhase();
        }
    }

    private void AdvanceToNextPhase()
    {
        if (_phase == PomodoroPhase.Work)
        {
            _completedWorkSessions++;
            var shouldTakeLongBreak = _completedWorkSessions % DefaultLongBreakInterval == 0;
            _phase = shouldTakeLongBreak ? PomodoroPhase.LongBreak : PomodoroPhase.ShortBreak;
            _phaseTotalSeconds = shouldTakeLongBreak ? _longBreakSeconds : _shortBreakSeconds;
            _remainingSeconds = _phaseTotalSeconds;
            return;
        }

        // After break, go back to work and stop like the macOS app.
        _phase = PomodoroPhase.Work;
        _phaseTotalSeconds = _workSeconds;
        _remainingSeconds = _workSeconds;
        _isRunning = false;
    }
}
