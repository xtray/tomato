namespace Tomato.WindowsCore;

public enum WindowsThemeMode
{
    WarmVivid,
    BusinessMotion,
    GreenFocus
}

public readonly record struct RgbAccent(byte R, byte G, byte B);

public readonly record struct PhaseAccents(
    RgbAccent Work,
    RgbAccent ShortBreak,
    RgbAccent LongBreak
);

public static class WindowsThemeCatalog
{
    public static WindowsThemeMode Next(WindowsThemeMode current)
    {
        return current switch
        {
            WindowsThemeMode.WarmVivid => WindowsThemeMode.BusinessMotion,
            WindowsThemeMode.BusinessMotion => WindowsThemeMode.GreenFocus,
            _ => WindowsThemeMode.WarmVivid
        };
    }

    public static PhaseAccents PhaseAccents(WindowsThemeMode mode)
    {
        return mode switch
        {
            WindowsThemeMode.BusinessMotion => new PhaseAccents(
                Work: new RgbAccent(51, 64, 79),
                ShortBreak: new RgbAccent(82, 138, 148),
                LongBreak: new RgbAccent(61, 94, 145)
            ),
            WindowsThemeMode.GreenFocus => new PhaseAccents(
                Work: new RgbAccent(49, 126, 93),
                ShortBreak: new RgbAccent(74, 168, 120),
                LongBreak: new RgbAccent(112, 196, 146)
            ),
            _ => new PhaseAccents(
                Work: new RgbAccent(224, 71, 56),
                ShortBreak: new RgbAccent(47, 167, 127),
                LongBreak: new RgbAccent(59, 122, 219)
            )
        };
    }

    public static RgbAccent PrimaryAccent(WindowsThemeMode mode)
    {
        return PhaseAccents(mode).Work;
    }
}
