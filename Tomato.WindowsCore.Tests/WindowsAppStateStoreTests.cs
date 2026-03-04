using System.Text;
using Tomato.WindowsCore;
using Xunit;

namespace Tomato.WindowsCore.Tests;

public sealed class WindowsAppStateStoreTests
{
    [Fact]
    public void SaveAndLoad_RoundTripsTasksAndSettings()
    {
        var tempPath = CreateTempFilePath();
        try
        {
            var store = new WindowsAppStateStore(tempPath);
            var taskId = Guid.NewGuid();
            var expected = new WindowsAppState
            {
                ThemeMode = WindowsThemeMode.BusinessMotion,
                WorkMinutes = 35,
                ShortBreakMinutes = 7,
                LongBreakMinutes = 20,
                FloatingWindowWidth = 368,
                FloatingWindowHeight = 452,
                Tasks =
                [
                    new WindowsTaskState { Id = taskId, Title = "Write proposal", CompletedPomodoros = 4 }
                ]
            };

            store.Save(expected);
            var loaded = store.Load();

            Assert.Equal(WindowsThemeMode.BusinessMotion, loaded.ThemeMode);
            Assert.Equal(35, loaded.WorkMinutes);
            Assert.Equal(7, loaded.ShortBreakMinutes);
            Assert.Equal(20, loaded.LongBreakMinutes);
            Assert.Equal(368, loaded.FloatingWindowWidth);
            Assert.Equal(452, loaded.FloatingWindowHeight);
            Assert.Single(loaded.Tasks);
            Assert.Equal(taskId, loaded.Tasks[0].Id);
            Assert.Equal("Write proposal", loaded.Tasks[0].Title);
            Assert.Equal(4, loaded.Tasks[0].CompletedPomodoros);
        }
        finally
        {
            CleanupTempPath(tempPath);
        }
    }

    [Fact]
    public void Load_WhenFileMissing_ReturnsDefaults()
    {
        var tempPath = CreateTempFilePath();
        CleanupTempPath(tempPath);

        var store = new WindowsAppStateStore(tempPath);
        var loaded = store.Load();

        Assert.Equal(WindowsThemeMode.WarmVivid, loaded.ThemeMode);
        Assert.Equal(25, loaded.WorkMinutes);
        Assert.Equal(5, loaded.ShortBreakMinutes);
        Assert.Equal(15, loaded.LongBreakMinutes);
        Assert.Equal(WindowsAppState.DefaultFloatingWindowWidth, loaded.FloatingWindowWidth);
        Assert.Equal(WindowsAppState.DefaultFloatingWindowHeight, loaded.FloatingWindowHeight);
        Assert.Empty(loaded.Tasks);
    }

    [Fact]
    public void Load_WhenFileContainsInvalidJson_ReturnsDefaults()
    {
        var tempPath = CreateTempFilePath();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);
            File.WriteAllText(tempPath, "not-json", Encoding.UTF8);

            var store = new WindowsAppStateStore(tempPath);
            var loaded = store.Load();

            Assert.Equal(WindowsThemeMode.WarmVivid, loaded.ThemeMode);
            Assert.Equal(25, loaded.WorkMinutes);
            Assert.Equal(5, loaded.ShortBreakMinutes);
            Assert.Equal(15, loaded.LongBreakMinutes);
            Assert.Equal(WindowsAppState.DefaultFloatingWindowWidth, loaded.FloatingWindowWidth);
            Assert.Equal(WindowsAppState.DefaultFloatingWindowHeight, loaded.FloatingWindowHeight);
            Assert.Empty(loaded.Tasks);
        }
        finally
        {
            CleanupTempPath(tempPath);
        }
    }

    [Fact]
    public void Load_NormalizesFloatingWindowSizeIntoSupportedRange()
    {
        var tempPath = CreateTempFilePath();
        try
        {
            var store = new WindowsAppStateStore(tempPath);
            store.Save(new WindowsAppState
            {
                FloatingWindowWidth = 120,
                FloatingWindowHeight = 140
            });

            var loaded = store.Load();

            Assert.Equal(WindowsAppState.MinFloatingWindowWidth, loaded.FloatingWindowWidth);
            Assert.Equal(WindowsAppState.MinFloatingWindowHeight, loaded.FloatingWindowHeight);
        }
        finally
        {
            CleanupTempPath(tempPath);
        }
    }

    private static string CreateTempFilePath()
    {
        var directory = Path.Combine(Path.GetTempPath(), "tomato-windowscore-tests", Guid.NewGuid().ToString("N"));
        return Path.Combine(directory, "state.json");
    }

    private static void CleanupTempPath(string tempPath)
    {
        var directory = Path.GetDirectoryName(tempPath);
        if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
