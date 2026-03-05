using Tomato.WindowsCore;
using Xunit;

namespace Tomato.WindowsCore.Tests;

public sealed class WindowsTaskReorderHelperTests
{
    [Fact]
    public void Reorder_MovesItemForward()
    {
        var items = new List<string> { "A", "B", "C", "D" };

        var moved = WindowsTaskReorderHelper.Reorder(items, fromIndex: 1, toIndex: 3);

        Assert.True(moved);
        Assert.Equal(new[] { "A", "C", "D", "B" }, items);
    }

    [Fact]
    public void Reorder_MovesItemBackward()
    {
        var items = new List<string> { "A", "B", "C", "D" };

        var moved = WindowsTaskReorderHelper.Reorder(items, fromIndex: 3, toIndex: 1);

        Assert.True(moved);
        Assert.Equal(new[] { "A", "D", "B", "C" }, items);
    }

    [Fact]
    public void Reorder_WhenTargetLessThanZero_ClampsToStart()
    {
        var items = new List<string> { "A", "B", "C" };

        var moved = WindowsTaskReorderHelper.Reorder(items, fromIndex: 2, toIndex: -5);

        Assert.True(moved);
        Assert.Equal(new[] { "C", "A", "B" }, items);
    }

    [Fact]
    public void Reorder_WhenTargetGreaterThanEnd_ClampsToEnd()
    {
        var items = new List<string> { "A", "B", "C" };

        var moved = WindowsTaskReorderHelper.Reorder(items, fromIndex: 0, toIndex: 99);

        Assert.True(moved);
        Assert.Equal(new[] { "B", "C", "A" }, items);
    }

    [Fact]
    public void Reorder_WhenSourceInvalid_ReturnsFalseAndKeepsOrder()
    {
        var items = new List<string> { "A", "B", "C" };

        var moved = WindowsTaskReorderHelper.Reorder(items, fromIndex: -1, toIndex: 1);

        Assert.False(moved);
        Assert.Equal(new[] { "A", "B", "C" }, items);
    }
}
