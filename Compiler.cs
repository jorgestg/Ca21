using System.Collections.Immutable;
using Ca21.Binding;
using Ca21.Diagnostics;
using Ca21.Symbols;

namespace Ca21;

internal sealed class Compiler
{
    private readonly DiagnosticList _diagnosticsBuilder = new();
    private readonly List<BoundBlock> _bodiesBuilder = new();

    private Compiler(ModuleSymbol moduleSymbol)
    {
        ModuleSymbol = moduleSymbol;
    }

    public ModuleSymbol ModuleSymbol { get; }

    private ImmutableArray<Diagnostic> _diagnostics;
    public ImmutableArray<Diagnostic> Diagnostics =>
        _diagnostics.IsDefault ? _diagnostics = _diagnosticsBuilder.DrainToImmutable() : _diagnostics;

    private ImmutableArray<BoundBlock> _bodies;
    public ImmutableArray<BoundBlock> Bodies =>
        _bodies.IsDefault ? _bodies = _bodiesBuilder.ToImmutableArray() : _bodies;

    public static Compiler Compile(ModuleSymbol moduleSymbol)
    {
        var compiler = new Compiler(moduleSymbol);
        compiler.Compile();
        return compiler;
    }

    private void Compile()
    {
        if (ModuleSymbol.Diagnostics.Any())
            AppendDiagnostics(ModuleSymbol.Diagnostics);

        foreach (var functionSymbol in ModuleSymbol.Functions)
            CompileFunction((SourceFunctionSymbol)functionSymbol);
    }

    private void CompileFunction(SourceFunctionSymbol functionSymbol)
    {
        var diagnostics = new DiagnosticList();
        var body = functionSymbol.Binder.BindBody(diagnostics);
        if (diagnostics.Any())
            AppendDiagnostics(diagnostics);

        _bodiesBuilder.Add(body);
    }

    private void AppendDiagnostics(DiagnosticList diagnostics)
    {
        foreach (var diagnostic in diagnostics)
            _diagnosticsBuilder.Add(diagnostic);
    }

    private void AppendDiagnostics(ImmutableArray<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
            _diagnosticsBuilder.Add(diagnostic);
    }
}
