using System.Collections.Frozen;
using System.Collections.Immutable;
using Antlr4.Runtime;
using Ca21.Binding;
using Ca21.Diagnostics;
using static Ca21.Antlr.Ca21Parser;

namespace Ca21.Symbols;

internal abstract class FunctionSymbol : Symbol
{
    public static new readonly FunctionSymbol Missing = new MissingFunctionSymbol();

    public abstract ModuleSymbol Module { get; }
    public abstract ImmutableArray<SourceParameterSymbol> Parameters { get; }
    public abstract TypeSymbol ReturnType { get; }

    private sealed class MissingFunctionSymbol : FunctionSymbol
    {
        public override ParserRuleContext Context => ParserRuleContext.EMPTY;
        public override ModuleSymbol Module => throw new InvalidOperationException();
        public override ImmutableArray<SourceParameterSymbol> Parameters => throw new InvalidOperationException();
        public override TypeSymbol ReturnType => TypeSymbol.BadType;
        public override string Name => "???";
    }
}

internal sealed class SourceFunctionSymbol : FunctionSymbol
{
    public SourceFunctionSymbol(FunctionDefinitionContext context, ModuleSymbol module)
    {
        Context = context;
        Binder = new FunctionBinder(this);
        Module = module;

        CreateParameters(this, out var diagnostics, out var parameters, out var parameterMap);
        Diagnostics = diagnostics;
        Parameters = parameters;
        ParameterMap = parameterMap;
    }

    public override FunctionDefinitionContext Context { get; }
    public override string Name => Context.Signature.Name.Text;
    public override FunctionBinder Binder { get; }

    public override ModuleSymbol Module { get; }
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    private TypeSymbol? _returnType;
    public override TypeSymbol ReturnType => _returnType ??= Binder.BindType(Context.Signature.ReturnType);

    public override ImmutableArray<SourceParameterSymbol> Parameters { get; }
    public FrozenDictionary<string, SourceParameterSymbol> ParameterMap { get; }

    private static void CreateParameters(
        SourceFunctionSymbol functionSymbol,
        out ImmutableArray<Diagnostic> diagnostics,
        out ImmutableArray<SourceParameterSymbol> parameters,
        out FrozenDictionary<string, SourceParameterSymbol> parameterMap
    )
    {
        var context = functionSymbol.Context.Signature.ParameterList?._Parameters;
        if (context == null)
        {
            diagnostics = ImmutableArray<Diagnostic>.Empty;
            parameters = ImmutableArray<SourceParameterSymbol>.Empty;
            parameterMap = FrozenDictionary<string, SourceParameterSymbol>.Empty;
            return;
        }

        var diagnosticsBuilder = new DiagnosticList();
        var parametersBuilder = new ArrayBuilder<SourceParameterSymbol>(context.Count);
        var mapBuilder = new Dictionary<string, SourceParameterSymbol>();
        foreach (var parameterContext in context)
        {
            var parameterSymbol = new SourceParameterSymbol(parameterContext, functionSymbol);
            if (!mapBuilder.TryAdd(parameterSymbol.Name, parameterSymbol))
            {
                diagnosticsBuilder.Add(
                    parameterContext.Name,
                    DiagnosticMessages.NameIsAlreadyDefined(functionSymbol.Name)
                );
            }

            parametersBuilder.Add(parameterSymbol);
        }

        diagnostics = diagnosticsBuilder.DrainToImmutable();
        parameters = parametersBuilder.MoveToImmutable();
        parameterMap = mapBuilder.ToFrozenDictionary();
    }
}
