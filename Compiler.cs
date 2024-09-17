using System.Collections.Frozen;
using System.Collections.Immutable;
using Ca21.Binding;
using Ca21.Diagnostics;
using Ca21.Symbols;

namespace Ca21;

internal sealed class Compiler
{
    private readonly DiagnosticList _diagnosticsBuilder = new();
    private readonly Dictionary<SourceFunctionSymbol, ControlFlowGraph> _bodiesBuilder = new();

    private Compiler(ModuleSymbol moduleSymbol)
    {
        ModuleSymbol = moduleSymbol;
    }

    public ModuleSymbol ModuleSymbol { get; }

    public ImmutableArray<Diagnostic> Diagnostics => _diagnosticsBuilder.GetImmutableArray();

    private FrozenDictionary<SourceFunctionSymbol, ControlFlowGraph>? _bodies;
    public FrozenDictionary<SourceFunctionSymbol, ControlFlowGraph> Bodies =>
        _bodies ??= _bodiesBuilder.ToFrozenDictionary();

    public static Compiler Compile(ModuleSymbol moduleSymbol)
    {
        var compiler = new Compiler(moduleSymbol);
        compiler.CompileModule();
        return compiler;
    }

    private void CompileModule()
    {
        _diagnosticsBuilder.AddRange(ModuleSymbol.Diagnostics);

        foreach (var structureSymbol in ModuleSymbol.GetMembers<StructureSymbol>())
            _diagnosticsBuilder.AddRange(structureSymbol.Diagnostics);

        foreach (var functionSymbol in ModuleSymbol.GetMembers<SourceFunctionSymbol>())
            CompileFunction(functionSymbol);
    }

    private void CompileFunction(SourceFunctionSymbol functionSymbol)
    {
        var cfg = functionSymbol.Binder.BindBody(_diagnosticsBuilder);
        if (cfg == null)
            return;

        _diagnosticsBuilder.AddRange(functionSymbol.Diagnostics);
        _bodiesBuilder.Add(functionSymbol, cfg);
    }
}
