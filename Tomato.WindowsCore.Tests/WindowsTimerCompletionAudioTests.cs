using Tomato.WindowsCore;
using Xunit;

namespace Tomato.WindowsCore.Tests;

public sealed class WindowsTimerCompletionAudioTests
{
    [Fact]
    public void Resolve_WhenWorkTransitionsIntoShortBreak_ReturnsWorkCompleted()
    {
        var before = new PomodoroSnapshot(PomodoroPhase.Work, 1, 60, 0, true);
        var after = new PomodoroSnapshot(PomodoroPhase.ShortBreak, 300, 300, 1, true);

        var result = WindowsTimerCompletionAudioEventResolver.Resolve(before, after);

        Assert.Equal(WindowsTimerCompletionAudioEvent.WorkCompleted, result);
    }

    [Fact]
    public void Resolve_WhenBreakTransitionsIntoStoppedWorkReady_ReturnsBreakCompleted()
    {
        var before = new PomodoroSnapshot(PomodoroPhase.LongBreak, 1, 900, 4, true);
        var after = new PomodoroSnapshot(PomodoroPhase.Work, 1500, 1500, 4, false);

        var result = WindowsTimerCompletionAudioEventResolver.Resolve(before, after);

        Assert.Equal(WindowsTimerCompletionAudioEvent.BreakCompleted, result);
    }

    [Fact]
    public void Resolve_WhenNoCompletionTransitionOccurred_ReturnsNone()
    {
        var before = new PomodoroSnapshot(PomodoroPhase.Work, 45, 60, 0, true);
        var after = new PomodoroSnapshot(PomodoroPhase.Work, 44, 60, 0, true);

        var result = WindowsTimerCompletionAudioEventResolver.Resolve(before, after);

        Assert.Equal(WindowsTimerCompletionAudioEvent.None, result);
    }

    [Fact]
    public void CreateWaveData_ProducesRiffHeaderAndDistinctMelodies()
    {
        var workData = WindowsCompletionChime.CreateWaveData(
            WindowsTimerCompletionAudioEvent.WorkCompleted,
            volume: 0.5D
        );
        var breakData = WindowsCompletionChime.CreateWaveData(
            WindowsTimerCompletionAudioEvent.BreakCompleted,
            volume: 0.5D
        );

        Assert.NotEmpty(workData);
        Assert.NotEmpty(breakData);
        Assert.Equal("RIFF", System.Text.Encoding.ASCII.GetString(workData, 0, 4));
        Assert.Equal("RIFF", System.Text.Encoding.ASCII.GetString(breakData, 0, 4));
        Assert.NotEqual(Convert.ToBase64String(workData), Convert.ToBase64String(breakData));
    }
}
