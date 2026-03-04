using Tomato.WindowsCore;
using Xunit;

namespace Tomato.WindowsCore.Tests;

public class WindowsThemeModeTests
{
    [Fact]
    public void Next_CyclesThroughThreeThemes()
    {
        Assert.Equal(WindowsThemeMode.BusinessMotion, WindowsThemeCatalog.Next(WindowsThemeMode.WarmVivid));
        Assert.Equal(WindowsThemeMode.GreenFocus, WindowsThemeCatalog.Next(WindowsThemeMode.BusinessMotion));
        Assert.Equal(WindowsThemeMode.WarmVivid, WindowsThemeCatalog.Next(WindowsThemeMode.GreenFocus));
    }

    [Fact]
    public void GreenFocus_UsesDistinctGreenConcentrationsForBreakPhases()
    {
        var accents = WindowsThemeCatalog.PhaseAccents(WindowsThemeMode.GreenFocus);

        Assert.NotEqual(accents.Work, accents.ShortBreak);
        Assert.NotEqual(accents.Work, accents.LongBreak);
        Assert.NotEqual(accents.ShortBreak, accents.LongBreak);

        Assert.True(accents.ShortBreak.G > accents.Work.G);
        Assert.True(accents.LongBreak.G > accents.Work.G);
    }

    [Fact]
    public void PrimaryAccent_TracksThemeMode()
    {
        var warm = WindowsThemeCatalog.PrimaryAccent(WindowsThemeMode.WarmVivid);
        var business = WindowsThemeCatalog.PrimaryAccent(WindowsThemeMode.BusinessMotion);
        var green = WindowsThemeCatalog.PrimaryAccent(WindowsThemeMode.GreenFocus);

        Assert.Equal((byte)224, warm.R);
        Assert.Equal((byte)51, business.R);
        Assert.Equal((byte)49, green.R);
        Assert.NotEqual(warm, business);
        Assert.NotEqual(warm, green);
        Assert.NotEqual(business, green);
    }
}
