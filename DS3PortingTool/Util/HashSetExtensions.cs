namespace DS3PortingTool.Util;

public static class HashSetExtensions
{
    /// <summary>
    /// Adds a collection of type T to a HashSet of type T.
    /// </summary>
    public static void AddRange<T>(this HashSet<T> set, IEnumerable<T> items)
    {
        foreach (T item in items)
        {
            set.Add(item);
        }
    }
}