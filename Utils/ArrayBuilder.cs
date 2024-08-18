using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Ca21;

/// <summary>
/// A stack-allocated, fixed-size, non-shareable <see cref="ImmutableArray{T}"/> builder.
/// </summary>
internal ref struct ArrayBuilder<T>
{
    private T[] _array;

    public ArrayBuilder(int capacity)
    {
        _array = new T[capacity];
    }

    public ArrayBuilder()
    {
        _array = [];
    }

    public readonly int Capacity => _array.Length;
    public int Count { get; private set; }

    public readonly bool IsDefault => _array == null;

    public void Add(T value)
    {
        if (_array.Length == Count)
            Array.Resize(ref _array, _array.Length == 0 ? 4 : _array.Length * 2);

        _array[Count++] = value;
    }

    public bool TryAdd(T value)
    {
        if (IsDefault || _array.Length == Count)
            return false;

        _array[Count++] = value;
        return true;
    }

    public readonly ImmutableArray<T> DrainToImmutable()
    {
        if (_array.Length == Count)
            return ImmutableCollectionsMarshal.AsImmutableArray(_array);

        return ImmutableArray.Create(_array, 0, Count);
    }

    public readonly ImmutableArray<T> MoveToImmutable()
    {
        if (_array.Length == Count)
            return ImmutableCollectionsMarshal.AsImmutableArray(_array);

        throw new InvalidOperationException("Can only move when Count matches Capacity.");
    }
}
