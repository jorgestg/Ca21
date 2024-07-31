using System.Collections;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace Ca21.Diagnostics;

public sealed class DiagnosticList
{
    private Diagnostic[]? _diagnostics;
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

    public bool Any() => _count > 0;

    public void Add(Diagnostic item)
    {
        if (_count == Capacity)
            Resize(Capacity == 0 ? 4 : Capacity * 2);

        _diagnostics![_count++] = item;
    }

    public ImmutableArray<Diagnostic> DrainToImmutable()
    {
        if (_diagnostics == null)
            return [];

        if (_count == Capacity)
            return ImmutableCollectionsMarshal.AsImmutableArray(_diagnostics);

        return ImmutableArray.Create(_diagnostics.AsSpan(0, _count));
    }

    private void Resize(int newCapacity)
    {
        var newArray = new Diagnostic[newCapacity];
        Array.Resize(ref _diagnostics, _count);
        _diagnostics = newArray;
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
