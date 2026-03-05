using Tomato.WindowsCore;
using Xunit;

namespace Tomato.WindowsCore.Tests;

public sealed class WindowsPomodoroBadgeFormatterTests
{
    [Fact]
    public void FormatTaskMeta_WhenNoCompletedPomodoros_ReturnsEmptyText()
    {
        Assert.Equal(string.Empty, WindowsPomodoroBadgeFormatter.FormatTaskMeta(0, WindowsAppLanguage.English));
        Assert.Equal(string.Empty, WindowsPomodoroBadgeFormatter.FormatTaskMeta(0, WindowsAppLanguage.Chinese));
    }

    [Fact]
    public void FormatTaskMeta_WhenCompletedPomodorosWithinFive_ShowsExactTomatoCount()
    {
        Assert.Equal(
            "3x🍅🍅🍅",
            WindowsPomodoroBadgeFormatter.FormatTaskMeta(3, WindowsAppLanguage.English)
        );
    }

    [Fact]
    public void FormatTaskMeta_WhenCompletedPomodorosMoreThanFive_CapsTomatoesAndAppendsPlus()
    {
        Assert.Equal(
            "8x🍅🍅🍅🍅🍅+",
            WindowsPomodoroBadgeFormatter.FormatTaskMeta(8, WindowsAppLanguage.English)
        );
    }
}
