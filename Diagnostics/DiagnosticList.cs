using System.Collections;

namespace Ca21.Diagnostics;

public readonly struct DiagnosticList() : IEnumerable<Diagnostic>, IReadOnlyList<Diagnostic>
{
    public static readonly DiagnosticList Empty = new();

    private readonly List<Diagnostic> _diagnostics = [];

    public int Count => _diagnostics.Count;

    public Diagnostic this[int index] => _diagnostics[index];

    public void Add(Diagnostic diagnostic) => _diagnostics.Add(diagnostic);

    IEnumerator<Diagnostic> IEnumerable<Diagnostic>.GetEnumerator() => _diagnostics.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _diagnostics.GetEnumerator();

    public List<Diagnostic>.Enumerator GetEnumerator() => _diagnostics.GetEnumerator();
}
