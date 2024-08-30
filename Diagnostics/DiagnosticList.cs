using System.Buffers;
using System.Collections;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Ca21.Diagnostics;

/// <summary>
/// A growable list of diagnostics.
/// </summary>
public sealed class DiagnosticList
{
    private Diagnostic[]? _diagnostics;
    private Diagnostic[]? _rentedArray;
    private int _count;

    public int Count => _count;
    public int Capacity => _diagnostics?.Length ?? 0;

    public Diagnostic this[int index]
    {
        get
        {
            if (_diagnostics == null || index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));

            return _diagnostics[index];
        }
        set
        {
            if (_diagnostics == null || index < 0 || index >= _count)
                throw new ArgumentOutOfRangeException(nameof(index));

            _diagnostics[index] = value;
        }
    }

    public void Add(Diagnostic item)
    {
        if (_count == Capacity)
            Resize(Capacity == 0 ? 1 : Capacity * 2);

        _diagnostics![_count++] = item;
    }

    public void AddRange(ImmutableArray<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
            Add(diagnostic);
    }

    public void AddRange(DiagnosticList diagnostics)
    {
        foreach (var diagnostic in diagnostics)
            Add(diagnostic);
    }

    private ImmutableArray<Diagnostic> _immutableArray;

    public ImmutableArray<Diagnostic> GetImmutableArray()
    {
        if (!_immutableArray.IsDefault && _immutableArray.Length == _count)
            return _immutableArray;

        if (_diagnostics == null)
            return _immutableArray = [];

        if (_rentedArray == null && _count == Capacity)
            return _immutableArray = ImmutableCollectionsMarshal.AsImmutableArray(_diagnostics);

        return _immutableArray = ImmutableArray.Create(_diagnostics, 0, _count);
    }

    private void Resize(int newCapacity)
    {
        if (_rentedArray != null)
            ArrayPool<Diagnostic>.Shared.Return(_rentedArray);

        _rentedArray = ArrayPool<Diagnostic>.Shared.Rent(newCapacity);
        _diagnostics = _rentedArray;
    }

    public Enumerator GetEnumerator() => new(this);

    public struct Enumerator(DiagnosticList diagnostics) : IEnumerator<Diagnostic>, IEnumerator
    {
        private readonly DiagnosticList _diagnostics = diagnostics;
        private int _index = -1;

        public readonly Diagnostic Current => _diagnostics[_index];

        readonly object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            _index++;
            return _index < _diagnostics.Count;
        }

        public void Reset()
        {
            _index = -1;
        }

        public readonly void Dispose() { }
    }
}
