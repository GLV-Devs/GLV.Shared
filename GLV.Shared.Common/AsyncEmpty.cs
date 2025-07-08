namespace GLV.Shared.Common;

public static class AsyncEmpty<T>
{
    private static IAsyncEnumerable<T>? empty;
    public static IAsyncEnumerable<T> Empty()
        => empty ??= EmptyIterator();

    private async static IAsyncEnumerable<T> EmptyIterator()
    {
        yield break;
    }
}
