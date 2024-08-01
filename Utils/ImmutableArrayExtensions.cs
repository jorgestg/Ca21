namespace System.Collections.Immutable;

public static class ImmutableArrayExtensions
{
    public static IEnumerable<(T1, T2)> Zip<T1, T2>(this ImmutableArray<T1> source, ImmutableArray<T2> other)
    {
        for (int i = 0; i < Math.Min(source.Length, other.Length); i++)
            yield return (source[i], other[i]);
    }
}
