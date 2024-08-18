using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Antlr4.Runtime;
using Ca21.Binding;
using Ca21.Diagnostics;
using static Ca21.Antlr.Ca21Parser;

namespace Ca21.Symbols;

internal abstract class FunctionSymbol : Symbol
{
    public static new readonly FunctionSymbol Missing = new MissingFunctionSymbol();

    public abstract ImmutableArray<SourceParameterSymbol> Parameters { get; }
    public abstract TypeSymbol ReturnType { get; }

    private sealed class MissingFunctionSymbol : FunctionSymbol
    {
        public override ParserRuleContext Context => ParserRuleContext.EMPTY;
        public override ImmutableArray<SourceParameterSymbol> Parameters => throw new InvalidOperationException();
        public override TypeSymbol ReturnType => TypeSymbol.BadType;
        public override string Name => "???";
    }
}

internal sealed class SourceFunctionSymbol : FunctionSymbol, IModuleMemberSymbol
{
    public SourceFunctionSymbol(FunctionDefinitionContext context, ModuleSymbol module)
    {
        Context = context;
        Binder = new FunctionBinder(this);
        ContainingModule = module;

        var parametersContext = Context.Signature.ParameterList?._Parameters;
        if (parametersContext == null)
        {
            Parameters = [];
            ParameterMap = FrozenDictionary<string, SourceParameterSymbol>.Empty;
            return;
        }

        var parametersBuilder = new ArrayBuilder<SourceParameterSymbol>(parametersContext.Count);
        var mapBuilder = new Dictionary<string, SourceParameterSymbol>();
        foreach (var parameterContext in parametersContext)
        {
            var parameterSymbol = new SourceParameterSymbol(parameterContext, this);
            if (!mapBuilder.TryAdd(parameterSymbol.Name, parameterSymbol))
                _diagnostics.Add(parameterContext.Name, DiagnosticMessages.NameIsAlreadyDefined(parameterSymbol.Name));

            parametersBuilder.Add(parameterSymbol);
        }

        Parameters = parametersBuilder.MoveToImmutable();
        ParameterMap = mapBuilder.ToFrozenDictionary();
    }

    public override FunctionDefinitionContext Context { get; }

    public override string Name => Context.Signature.Name.Text;
    public override FunctionBinder Binder { get; }

    private TypeSymbol? _type;
    public override TypeSymbol Type => _type ??= new FunctionTypeSymbol(this);

    private readonly DiagnosticList _diagnostics = new();
    public override ImmutableArray<Diagnostic> Diagnostics => _diagnostics.GetImmutableArray();

    public ModuleSymbol ContainingModule { get; }

    public override ImmutableArray<SourceParameterSymbol> Parameters { get; }

    private TypeSymbol? _returnType;
    public override TypeSymbol ReturnType
    {
        get
        {
            if (_returnType != null)
                return _returnType;

            if (Context.Signature.ReturnType == null)
                _returnType = TypeSymbol.Unit;
            else
                _returnType = Binder.BindType(Context.Signature.ReturnType, _diagnostics);

            return _returnType;
        }
    }

    public FrozenDictionary<string, SourceParameterSymbol> ParameterMap { get; }

    public bool IsExported => Context.ExportModifier != null;

    [MemberNotNullWhen(true, nameof(ExternName))]
    public bool IsExtern => Context.ExternModifier != null;

    public string? ExternName => Context.ExternModifier.ExternName?.Text;
}
