using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Ca21;

public static class ImmutableArrayExtensions
{
    /// <summary>
    /// Casts an <see cref="ImmutableArray{T}"/> to an <see cref="IEnumerable{T}"/>
    /// without boxing it.
    /// </summary>
    public static IEnumerable<T> AsEnumerable<T>(this ImmutableArray<T> source) =>
        ImmutableCollectionsMarshal.AsArray(source)!;
}
