namespace Tomato.WindowsCore;

public static class WindowsTaskbarWindowStyles
{
    public const int SystemMenu = 0x00080000;
    public const int MaximizeBox = 0x00010000;
    public const int MinimizeBox = 0x00020000;

    public static int EnsureTaskbarToggleStyles(int style)
        => style | SystemMenu | MaximizeBox | MinimizeBox;
}
