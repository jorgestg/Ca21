using System.Collections.Immutable;
using System.Diagnostics;
using Ca21.Diagnostics;
using static Ca21.Antlr.Ca21Parser;

namespace Ca21.Symbols;

internal sealed class ModuleSymbol(CompilationUnitContext context) : Symbol
{
    public override CompilationUnitContext Context { get; } = context;
    public override string Name => "module";

    private DiagnosticList? _diagnostics;
    public ImmutableArray<Diagnostic> Diagnostics => _diagnostics == null ? [] : _diagnostics.DrainToImmutable();

    private ImmutableArray<FunctionSymbol> _functions;
    public ImmutableArray<FunctionSymbol> Functions =>
        _functions.IsDefault ? _functions = CreateFunctions() : _functions;

    private ImmutableArray<FunctionSymbol> CreateFunctions()
    {
        var seenNames = new string[Context._Functions.Count];
        var builder = new ArrayBuilder<FunctionSymbol>(Context._Functions.Count);
        foreach (var context in Context._Functions)
        {
            var functionSymbol = context switch
            {
                FunctionDefinitionContext c => new SourceFunctionSymbol(context, this),
                _ => throw new UnreachableException()
            };

            if (seenNames.Contains(functionSymbol.Name))
            {
                _diagnostics ??= new DiagnosticList();
                _diagnostics.Add(context, DiagnosticMessages.NameIsAlreadyDefined(functionSymbol.Name));
            }

            builder.Add(functionSymbol);
        }

        return builder.MoveToImmutable();
    }
}
