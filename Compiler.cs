using System.Collections.Frozen;
using System.Collections.Immutable;
using Ca21.Binding;
using Ca21.Diagnostics;
using Ca21.Symbols;

namespace Ca21;

internal sealed class Compiler
{
    private readonly DiagnosticList _diagnosticsBuilder = new();
    private readonly Dictionary<FunctionSymbol, BoundBlock> _bodiesBuilder = new();

    private Compiler(ModuleSymbol moduleSymbol)
    {
        ModuleSymbol = moduleSymbol;
    }

    public ModuleSymbol ModuleSymbol { get; }

    private ImmutableArray<Diagnostic> _diagnostics;
    public ImmutableArray<Diagnostic> Diagnostics =>
        _diagnostics.IsDefault ? _diagnostics = _diagnosticsBuilder.DrainToImmutable() : _diagnostics;

    private FrozenDictionary<FunctionSymbol, BoundBlock>? _bodies;
    public FrozenDictionary<FunctionSymbol, BoundBlock> Bodies => _bodies ??= _bodiesBuilder.ToFrozenDictionary();

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
        if (functionSymbol.Diagnostics.Any())
            AppendDiagnostics(functionSymbol.Diagnostics);

        if (functionSymbol.IsExtern)
            return;

        var diagnostics = new DiagnosticList();
        var body = functionSymbol.Binder.BindBody(diagnostics);
        if (diagnostics.Any())
            AppendDiagnostics(diagnostics);

        _bodiesBuilder.Add(functionSymbol, body);
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
