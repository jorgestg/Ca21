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
        _diagnostics.IsDefault ? _diagnostics = _diagnosticsBuilder.GetImmutableArray() : _diagnostics;

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
            _diagnosticsBuilder.AddRange(ModuleSymbol.Diagnostics);

        foreach (var structureSymbol in ModuleSymbol.Structures)
            CompileStructure(structureSymbol);

        foreach (var functionSymbol in ModuleSymbol.Functions)
            CompileFunction(functionSymbol);
    }

    private void CompileStructure(StructureSymbol structureSymbol)
    {
        if (structureSymbol.Diagnostics.Any())
            _diagnosticsBuilder.AddRange(structureSymbol.Diagnostics);
    }

    private void CompileFunction(SourceFunctionSymbol functionSymbol)
    {
        if (functionSymbol.Diagnostics.Any())
            _diagnosticsBuilder.AddRange(functionSymbol.Diagnostics);

        var diagnostics = new DiagnosticList();
        var body = functionSymbol.Binder.BindBody(diagnostics);
        if (diagnostics.Any())
            _diagnosticsBuilder.AddRange(diagnostics);

        _bodiesBuilder.Add(functionSymbol, body);
    }
}
