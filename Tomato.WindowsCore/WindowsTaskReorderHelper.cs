namespace Tomato.WindowsCore;

public static class WindowsTaskReorderHelper
{
    public static bool Reorder<T>(IList<T> items, int fromIndex, int toIndex)
    {
        ArgumentNullException.ThrowIfNull(items);

        if (items.Count <= 1 || fromIndex < 0 || fromIndex >= items.Count)
        {
            return false;
        }

        var destinationIndex = Math.Clamp(toIndex, 0, items.Count - 1);
        if (destinationIndex == fromIndex)
        {
            return false;
        }

        var item = items[fromIndex];
        items.RemoveAt(fromIndex);
        items.Insert(destinationIndex, item);
        return true;
    }
}
