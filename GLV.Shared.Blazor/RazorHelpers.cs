namespace GLV.Shared.Blazor;

public static class RazorHelpers
{
    public static IEnumerable<(T, int)> GroupByElementIndex<T>(this IEnumerable<T> values, int grouping = 2)
    {
        int c = 0;
        foreach (var x in values)
            yield return (x, c++ % grouping);
    }

    public static IEnumerable<(TKey, TValue, int)> GroupByElementIndex<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> values, int grouping = 2)
    {
        int c = 0;
        foreach (var (k, v) in values)
            yield return (k, v, c++ % grouping);
    }
}
