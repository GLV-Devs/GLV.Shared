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
    public static T GetRandomItem<T>(this IEnumerable<T> collection, Random? rand)
        => collection.ElementAt((rand ?? Random.Shared).Next(0, collection.Count()));

    public static T GetRandomItem<T>(this IEnumerable<T> collection)
        => collection.ElementAt(RandomNumberGenerator.GetInt32(0, collection.Count()));
}
