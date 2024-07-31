using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Ca21;

/// <summary>
/// A stack-allocated, fixed-size, non-shareable <see cref="ImmutableArray{T}"/> builder.
/// </summary>
internal ref struct ArrayBuilder<T>(int capacity)
{
    private readonly T[] _array = new T[capacity];

    public int Count { get; private set; }
    public int Capacity { get; } = capacity;

    public readonly bool IsDefault => _array == null;

    public void Add(T value)
    {
        _array[Count++] = value;
    }

    public readonly ImmutableArray<T> MoveToImmutable() => ImmutableCollectionsMarshal.AsImmutableArray(_array);
}
