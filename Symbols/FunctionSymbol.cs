using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
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
    private FunctionSignatureContext SignatureContext =>
        Context switch
        {
            TopLevelFunctionDefinitionContext c => c.Signature,
            ExternFunctionDefinitionContext c => c.Signature,
            _ => throw new UnreachableException()
        };

    public override string Name => SignatureContext.Name.Text;
    public override FunctionBinder Binder { get; }

    private TypeSymbol? _type;
    public override TypeSymbol Type => _type ??= new FunctionTypeSymbol(this);

    public override ModuleSymbol Module { get; }
    public ImmutableArray<Diagnostic> Diagnostics { get; }

    public override ImmutableArray<SourceParameterSymbol> Parameters { get; }

    private TypeSymbol? _returnType;
    public override TypeSymbol ReturnType
    {
        get
        {
            if (_returnType != null)
                return _returnType;

            if (SignatureContext.ReturnType == null)
                _returnType = TypeSymbol.Unit;
            else
                _returnType = Binder.BindType(SignatureContext.ReturnType);

            return _returnType;
        }
    }

    public FrozenDictionary<string, SourceParameterSymbol> ParameterMap { get; }

    public bool IsExported => Context is TopLevelFunctionDefinitionContext topLevel && topLevel.ExportModifier != null;

    public bool IsExtern => Context is ExternFunctionDefinitionContext;
    public string? ExternName => Context is ExternFunctionDefinitionContext c ? c.ExternName.Text : null;

    private static void CreateParameters(
        SourceFunctionSymbol functionSymbol,
        out ImmutableArray<Diagnostic> diagnostics,
        out ImmutableArray<SourceParameterSymbol> parameters,
        out FrozenDictionary<string, SourceParameterSymbol> parameterMap
    )
    {
        var context = functionSymbol.SignatureContext.ParameterList?._Parameters;
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
                    DiagnosticMessages.NameIsAlreadyDefined(parameterSymbol.Name)
                );
            }

            parametersBuilder.Add(parameterSymbol);
        }

        diagnostics = diagnosticsBuilder.DrainToImmutable();
        parameters = parametersBuilder.MoveToImmutable();
        parameterMap = mapBuilder.ToFrozenDictionary();
    }
}
