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

    public ImmutableArray<Diagnostic> Diagnostics => _diagnosticsBuilder.GetImmutableArray();

    private FrozenDictionary<FunctionSymbol, BoundBlock>? _bodies;
    public FrozenDictionary<FunctionSymbol, BoundBlock> Bodies => _bodies ??= _bodiesBuilder.ToFrozenDictionary();

    public static Compiler Compile(ModuleSymbol moduleSymbol)
    {
        var compiler = new Compiler(moduleSymbol);
        compiler.CompileModule();
        return compiler;
    }

    private void CompileModule()
    {
        _diagnosticsBuilder.AddRange(ModuleSymbol.Diagnostics);

        foreach (var structureSymbol in ModuleSymbol.Structures)
            _diagnosticsBuilder.AddRange(structureSymbol.Diagnostics);

        foreach (var functionSymbol in ModuleSymbol.Functions)
            CompileFunction(functionSymbol);
    }

    private void CompileFunction(SourceFunctionSymbol functionSymbol)
    {
        _diagnosticsBuilder.AddRange(functionSymbol.Diagnostics);

        var diagnostics = new DiagnosticList();
        var body = functionSymbol.Binder.BindBody(diagnostics);
        if (functionSymbol.IsExtern)
            return;

        _diagnosticsBuilder.AddRange(diagnostics);
        _bodiesBuilder.Add(functionSymbol, body);
    }
}
