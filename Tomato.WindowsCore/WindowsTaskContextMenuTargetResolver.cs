namespace Tomato.WindowsCore;

public static class WindowsTaskContextMenuTargetResolver
{
    public static Guid? Resolve(Guid? fallbackTaskId, object? itemTag, object? ownerTag, Guid? selectedTaskId)
    {
        if (itemTag is Guid itemId)
        {
            return itemId;
        }

        if (ownerTag is Guid ownerId)
        {
            return ownerId;
        }

        return fallbackTaskId ?? selectedTaskId;
    }
}
