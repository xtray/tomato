using Tomato.WindowsCore;
using Xunit;

namespace Tomato.WindowsCore.Tests;

public class WindowsTaskbarWindowStylesTests
{
    [Fact]
    public void EnsureTaskbarToggleStyles_AddsSystemMenuAndMinMaxBits()
    {
        const int originalStyle = 0;

        var style = WindowsTaskbarWindowStyles.EnsureTaskbarToggleStyles(originalStyle);

        Assert.NotEqual(originalStyle, style);
        Assert.True((style & WindowsTaskbarWindowStyles.SystemMenu) != 0);
        Assert.True((style & WindowsTaskbarWindowStyles.MinimizeBox) != 0);
        Assert.True((style & WindowsTaskbarWindowStyles.MaximizeBox) != 0);
    }

    [Fact]
    public void EnsureTaskbarToggleStyles_PreservesExistingStyleBits()
    {
        const int existingFlag = 0x04000000;

        var style = WindowsTaskbarWindowStyles.EnsureTaskbarToggleStyles(existingFlag);

        Assert.True((style & existingFlag) != 0);
    }
}
