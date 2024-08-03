using System.Collections.Frozen;
using System.Collections.Immutable;
using Ca21.Binding;
using Ca21.Diagnostics;
using static Ca21.Antlr.Ca21Parser;

namespace Ca21.Symbols;

internal sealed class ModuleSymbol : Symbol
{
    public ModuleSymbol(CompilationUnitContext context, string name)
    {
        Context = context;
        Name = name;
        Binder = new ModuleBinder(this);

        Initialize(this, out var diagnostics, out var functions, out var memberMap);
        Diagnostics = diagnostics;
        Functions = functions;
        MemberMap = memberMap;
    }

    public override CompilationUnitContext Context { get; }
    public override Binder Binder { get; }
    public override string Name { get; }
    public ImmutableArray<Diagnostic> Diagnostics { get; }
    public ImmutableArray<FunctionSymbol> Functions { get; }
    public FrozenDictionary<string, FunctionSymbol> MemberMap { get; }

    private static void Initialize(
        ModuleSymbol moduleSymbol,
        out ImmutableArray<Diagnostic> diagnostics,
        out ImmutableArray<FunctionSymbol> functions,
        out FrozenDictionary<string, FunctionSymbol> memberMap
    )
    {
        var diagnosticsBuilder = new DiagnosticList();
        var functionsBuilder = new ArrayBuilder<FunctionSymbol>(moduleSymbol.Context._Functions.Count);
        var memberMapBuilder = new Dictionary<string, FunctionSymbol>();
        foreach (var functionContext in moduleSymbol.Context._Functions)
        {
            var functionSymbol = new SourceFunctionSymbol(functionContext, moduleSymbol);
            functionsBuilder.Add(functionSymbol);

            var signature = functionContext switch
            {
                TopLevelFunctionDefinitionContext c => c.Signature,
                ExternFunctionDefinitionContext c => c.Signature,
                _ => throw new InvalidOperationException()
            };

            if (!memberMapBuilder.TryAdd(functionSymbol.Name, functionSymbol))
                diagnosticsBuilder.Add(signature.Name, DiagnosticMessages.NameIsAlreadyDefined(functionSymbol.Name));
        }

        diagnostics = diagnosticsBuilder.DrainToImmutable();
        functions = functionsBuilder.MoveToImmutable();
        memberMap = memberMapBuilder.ToFrozenDictionary();
    }
}
