using Tomato.WindowsCore;
using Xunit;

namespace Tomato.WindowsCore.Tests;

public class PomodoroEngineTests
{
    [Fact]
    public void StartFocusSession_InitializesWorkPhase()
    {
        var engine = new PomodoroEngine();

        engine.StartFocusSession(workMinutes: 1, shortBreakMinutes: 1, longBreakMinutes: 2);

        var state = engine.Snapshot;
        Assert.Equal(PomodoroPhase.Work, state.Phase);
        Assert.Equal(60, state.RemainingSeconds);
        Assert.Equal(60, state.PhaseTotalSeconds);
        Assert.True(state.IsRunning);
    }

    [Fact]
    public void Tick_AfterWork_TransitionsToShortBreakByDefault()
    {
        var engine = new PomodoroEngine();
        engine.StartFocusSession(workMinutes: 1, shortBreakMinutes: 1, longBreakMinutes: 2);

        for (var i = 0; i < 60; i++)
        {
            engine.Tick();
        }

        var state = engine.Snapshot;
        Assert.Equal(PomodoroPhase.ShortBreak, state.Phase);
        Assert.Equal(60, state.RemainingSeconds);
        Assert.True(state.IsRunning);
        Assert.Equal(1, state.CompletedWorkSessions);
    }

    [Fact]
    public void Tick_AfterFourthWork_TransitionsToLongBreak()
    {
        var engine = new PomodoroEngine();

        for (var round = 0; round < 4; round++)
        {
            engine.StartFocusSession(workMinutes: 1, shortBreakMinutes: 1, longBreakMinutes: 2);
            for (var i = 0; i < 60; i++)
            {
                engine.Tick();
            }

            if (round < 3)
            {
                for (var i = 0; i < 60; i++)
                {
                    engine.Tick();
                }
            }
        }

        var state = engine.Snapshot;
        Assert.Equal(PomodoroPhase.LongBreak, state.Phase);
        Assert.Equal(120, state.RemainingSeconds);
        Assert.Equal(4, state.CompletedWorkSessions);
    }

    [Fact]
    public void Tick_AfterBreakCompletion_ReturnsToWorkAndStops()
    {
        var engine = new PomodoroEngine();
        engine.StartFocusSession(workMinutes: 1, shortBreakMinutes: 1, longBreakMinutes: 2);

        for (var i = 0; i < 60; i++)
        {
            engine.Tick();
        }
        for (var i = 0; i < 60; i++)
        {
            engine.Tick();
        }

        var state = engine.Snapshot;
        Assert.Equal(PomodoroPhase.Work, state.Phase);
        Assert.Equal(60, state.RemainingSeconds);
        Assert.False(state.IsRunning);
    }
}
