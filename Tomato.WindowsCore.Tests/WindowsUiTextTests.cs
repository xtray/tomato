using Tomato.WindowsCore;
using Xunit;

namespace Tomato.WindowsCore.Tests;

public sealed class WindowsUiTextTests
{
    [Fact]
    public void Get_ReturnsExpectedTaskContextTextsPerLanguage()
    {
        Assert.Equal("Mark as Completed", WindowsUiText.Get("task.mark.done", WindowsAppLanguage.English));
        Assert.Equal("Mark as Incomplete", WindowsUiText.Get("task.mark.undone", WindowsAppLanguage.English));
        Assert.Equal("Delete Task", WindowsUiText.Get("task.delete.current", WindowsAppLanguage.English));

        Assert.Equal("标记完成", WindowsUiText.Get("task.mark.done", WindowsAppLanguage.Chinese));
        Assert.Equal("标记未完成", WindowsUiText.Get("task.mark.undone", WindowsAppLanguage.Chinese));
        Assert.Equal("删除任务", WindowsUiText.Get("task.delete.current", WindowsAppLanguage.Chinese));
    }

    [Fact]
    public void Get_FormatsTaskCompletionCount()
    {
        Assert.Equal("3 pomodoros completed", WindowsUiText.Get("task.completed.count", WindowsAppLanguage.English, 3));
        Assert.Equal("已完成 3 个番茄钟", WindowsUiText.Get("task.completed.count", WindowsAppLanguage.Chinese, 3));
    }

    [Fact]
    public void Get_ReturnsPausedStatusTextPerLanguage()
    {
        Assert.Equal("Paused", WindowsUiText.Get("timer.status.paused", WindowsAppLanguage.English));
        Assert.Equal("已暂停", WindowsUiText.Get("timer.status.paused", WindowsAppLanguage.Chinese));
    }

    [Fact]
    public void Get_UnknownKeyFallsBackToKey()
    {
        Assert.Equal("missing.key", WindowsUiText.Get("missing.key", WindowsAppLanguage.English));
    }
}
