namespace Tomato.WindowsCore;

public static class WindowsPomodoroBadgeFormatter
{
    public static string FormatTaskMeta(int completedPomodoros, WindowsAppLanguage language)
    {
        if (completedPomodoros <= 0)
        {
            _ = language;
            return string.Empty;
        }

        var visibleTomatoCount = Math.Min(completedPomodoros, 5);
        var tomatoes = string.Concat(Enumerable.Repeat("🍅", visibleTomatoCount));
        var overflow = completedPomodoros > 5 ? "+" : string.Empty;
        return $"{completedPomodoros}x{tomatoes}{overflow}";
    }
}
