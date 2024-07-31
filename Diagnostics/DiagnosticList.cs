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

    public void CopyTo(ICollection<Diagnostic> other)
    {
        if (_diagnostics == null)
            return;

        for (var i = 0; i < _count; i++)
            other.Add(_diagnostics[i]);
    }
}
