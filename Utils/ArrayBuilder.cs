using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Ca21;

/// <summary>
/// A stack-allocated <see cref="ImmutableArray{T}"/> builder.
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
        {
            var newCapacity = _array.Length == 0 ? 4 : _array.Length * 2;
            Array.Resize(ref _array, newCapacity);
        }

        _array[Count++] = value;
    }

    public void TryAdd(T value)
    {
        if (IsDefault || _array.Length == Count)
            return;

        _array[Count++] = value;
    }

    public ImmutableArray<T> DrainToImmutable()
    {
        ImmutableArray<T> immutableArray;
        if (_array.Length == Count)
            immutableArray = ImmutableCollectionsMarshal.AsImmutableArray(_array);
        else
            immutableArray = ImmutableArray.Create(_array, 0, Count);

        _array = [];
        Count = 0;
        return immutableArray;
    }

    public ImmutableArray<T> MoveToImmutable()
    {
        if (_array.Length != Count)
            throw new InvalidOperationException("Can only move when Count matches Capacity.");

        var immutableArray = ImmutableCollectionsMarshal.AsImmutableArray(_array);
        _array = [];
        Count = 0;
        return immutableArray;
    }
}
