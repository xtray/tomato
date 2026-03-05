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
                FloatingWindowOpacity = 0.82D,
                AppLanguage = WindowsAppLanguage.Chinese,
                Tasks =
                [
                    new WindowsTaskState
                    {
                        Id = taskId,
                        Title = "Write proposal",
                        CompletedPomodoros = 4,
                        IsCompleted = true
                    }
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
            Assert.Equal(0.82D, loaded.FloatingWindowOpacity, precision: 3);
            Assert.Equal(WindowsAppLanguage.Chinese, loaded.AppLanguage);
            Assert.Single(loaded.Tasks);
            Assert.Equal(taskId, loaded.Tasks[0].Id);
            Assert.Equal("Write proposal", loaded.Tasks[0].Title);
            Assert.Equal(4, loaded.Tasks[0].CompletedPomodoros);
            Assert.True(loaded.Tasks[0].IsCompleted);
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
        Assert.Equal(WindowsAppState.DefaultFloatingWindowOpacity, loaded.FloatingWindowOpacity, precision: 3);
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
            Assert.Equal(WindowsAppState.DefaultFloatingWindowOpacity, loaded.FloatingWindowOpacity, precision: 3);
            Assert.Empty(loaded.Tasks);
        }
        finally
        {
            CleanupTempPath(tempPath);
        }
    }

    [Fact]
    public void Load_WhenAppLanguageIsInvalid_FallsBackToEnglish()
    {
        var tempPath = CreateTempFilePath();
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);
            File.WriteAllText(
                tempPath,
                """
                {
                  "WorkMinutes": 33,
                  "AppLanguage": 99
                }
                """,
                Encoding.UTF8
            );

            var store = new WindowsAppStateStore(tempPath);
            var loaded = store.Load();

            Assert.Equal(33, loaded.WorkMinutes);
            Assert.Equal(WindowsAppLanguage.English, loaded.AppLanguage);
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

    [Fact]
    public void Load_NormalizesFloatingWindowOpacityIntoSupportedRange()
    {
        var tempPath = CreateTempFilePath();
        try
        {
            var store = new WindowsAppStateStore(tempPath);
            store.Save(new WindowsAppState
            {
                FloatingWindowOpacity = 0.15D
            });

            var loaded = store.Load();

            Assert.Equal(WindowsAppState.MinFloatingWindowOpacity, loaded.FloatingWindowOpacity, precision: 3);
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
