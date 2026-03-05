using Tomato.WindowsCore;
using Xunit;

namespace Tomato.WindowsCore.Tests;

public sealed class WindowsTaskContextMenuTargetResolverTests
{
    [Fact]
    public void Resolve_PrefersMenuItemTag()
    {
        var fallback = Guid.NewGuid();
        var itemTag = Guid.NewGuid();
        var ownerTag = Guid.NewGuid();

        var resolved = WindowsTaskContextMenuTargetResolver.Resolve(
            fallback,
            itemTag,
            ownerTag,
            selectedTaskId: Guid.NewGuid()
        );

        Assert.Equal(itemTag, resolved);
    }

    [Fact]
    public void Resolve_UsesOwnerTagWhenMenuItemTagMissing()
    {
        var fallback = Guid.NewGuid();
        var ownerTag = Guid.NewGuid();

        var resolved = WindowsTaskContextMenuTargetResolver.Resolve(
            fallback,
            itemTag: null,
            ownerTag,
            selectedTaskId: Guid.NewGuid()
        );

        Assert.Equal(ownerTag, resolved);
    }

    [Fact]
    public void Resolve_FallsBackToStoredContextTaskId()
    {
        var fallback = Guid.NewGuid();

        var resolved = WindowsTaskContextMenuTargetResolver.Resolve(
            fallback,
            itemTag: null,
            ownerTag: null,
            selectedTaskId: null
        );

        Assert.Equal(fallback, resolved);
    }

    [Fact]
    public void Resolve_FallsBackToSelectedTaskIdWhenContextIsMissing()
    {
        var selectedTaskId = Guid.NewGuid();

        var resolved = WindowsTaskContextMenuTargetResolver.Resolve(
            fallbackTaskId: null,
            itemTag: null,
            ownerTag: null,
            selectedTaskId: selectedTaskId
        );

        Assert.Equal(selectedTaskId, resolved);
    }
}
