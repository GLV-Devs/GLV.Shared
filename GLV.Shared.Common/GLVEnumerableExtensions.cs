using System.Diagnostics;
using System.Security.Cryptography;

namespace GLV.Shared.Common;

public static class FileExtensions
{
    public static long GetFileLength(string path)
    {
        using var f = File.OpenRead(path);
        return f.Length;
    }
}

public static class GLVEnumerableExtensions
{
    public static T GetRandomItem<T>(this T[] array, Random? rand)
        => array[(rand ?? Random.Shared).Next(0, array.Length)];

    public static T GetRandomItem<T>(this T[] array)
        => array[RandomNumberGenerator.GetInt32(0, array.Length)];

    public static T GetRandomItem<T>(this IEnumerable<T> collection, Random? rand)
        => collection.ElementAt((rand ?? Random.Shared).Next(0, collection.Count()));

    public static T GetRandomItem<T>(this IEnumerable<T> collection)
        => collection.ElementAt(RandomNumberGenerator.GetInt32(0, collection.Count()));

    public static IEnumerable<ArraySegment<T>> ChunkInSegments<T>(this IEnumerable<T> items, int grouping = 2, bool fillInRow = false)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(grouping, 1);

        int idx = 0;
        T[] buffer = new T[grouping];
        foreach (var item in items)
        {
            buffer[idx++] = item;

            Debug.Assert(idx <= grouping);
            if (idx == grouping)
            {
                idx = 0;
                yield return new ArraySegment<T>(buffer, 0, grouping);
            }
        }

        Debug.Assert(idx <= grouping);
        if (idx > 0)
        {
            if (fillInRow)
            {
                for (int i = grouping - 1; i >= idx; i--)
                    buffer[i] = default!;
                idx = grouping;
            }

            yield return new ArraySegment<T>(buffer, 0, idx);
        }
    }
}
