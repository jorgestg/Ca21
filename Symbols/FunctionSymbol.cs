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

    public override SymbolKind Kind => SymbolKind.Function;
    public abstract ImmutableArray<SourceParameterSymbol> Parameters { get; }
    public abstract TypeSymbol ReturnType { get; }

    private sealed class MissingFunctionSymbol : FunctionSymbol
    {
        public override ParserRuleContext Context => ParserRuleContext.EMPTY;
        public override ImmutableArray<SourceParameterSymbol> Parameters => throw new InvalidOperationException();
        public override TypeSymbol ReturnType => TypeSymbol.Missing;
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
    }

    public override FunctionDefinitionContext Context { get; }

    public override string Name => Context.Signature.Name.Text;

    private TypeSymbol? _type;
    public override TypeSymbol Type => _type ??= new FunctionTypeSymbol(this);

    private ImmutableArray<Diagnostic> _diagnostics;
    public ImmutableArray<Diagnostic> Diagnostics
    {
        get
        {
            if (_diagnostics.IsDefault)
                InitializeProperties();

            return _diagnostics;
        }
    }

    private ImmutableArray<SourceParameterSymbol> _parameters;
    public override ImmutableArray<SourceParameterSymbol> Parameters
    {
        get
        {
            if (_parameters.IsDefault)
                InitializeProperties();

            return _parameters;
        }
    }

    private TypeSymbol? _returnType;
    public override TypeSymbol ReturnType
    {
        get
        {
            if (_returnType == null)
                InitializeProperties();

            return _returnType;
        }
    }

    public ModuleSymbol ContainingModule { get; }
    public FunctionBinder Binder { get; }

    private FrozenDictionary<string, SourceParameterSymbol>? _parameterMap;
    public FrozenDictionary<string, SourceParameterSymbol> ParameterMap
    {
        get
        {
            if (_parameterMap == null)
                InitializeProperties();

            return _parameterMap;
        }
    }

    public bool IsExported => Context.ExportModifier != null;
    public bool IsExtern => Context.ExternModifier != null;

    private string? _externName;
    public string? ExternName => _externName ??= Context.ExternModifier?.ExternName?.Text.Trim('"');

    [MemberNotNull(nameof(_returnType), nameof(_parameterMap))]
    private void InitializeProperties()
    {
        DiagnosticList? diagnostics = null;

        var returnTypeContext = Context.Signature.ReturnType;
        if (returnTypeContext == null)
        {
            _returnType = TypeSymbol.Unit;
        }
        else
        {
            diagnostics = new DiagnosticList();
            _returnType = Binder.BindType(returnTypeContext, diagnostics);
        }

        var parametersContext = Context.Signature.ParameterList?._Parameters;
        if (parametersContext == null)
        {
            _diagnostics = diagnostics?.GetImmutableArray() ?? [];
            _parameters = [];
            _parameterMap = FrozenDictionary<string, SourceParameterSymbol>.Empty;
            return;
        }

        diagnostics ??= new DiagnosticList();
        var parametersBuilder = new ArrayBuilder<SourceParameterSymbol>(parametersContext.Count);
        var mapBuilder = new Dictionary<string, SourceParameterSymbol>();
        foreach (var parameterContext in parametersContext)
        {
            var type = Binder.BindType(parameterContext.Type, diagnostics);
            var parameterSymbol = new SourceParameterSymbol(parameterContext, this, type);
            if (!mapBuilder.TryAdd(parameterSymbol.Name, parameterSymbol))
                diagnostics.Add(parameterContext.Name, DiagnosticMessages.NameIsAlreadyDefined(parameterSymbol.Name));

            parametersBuilder.Add(parameterSymbol);
        }

        _diagnostics = diagnostics.GetImmutableArray();
        _parameters = parametersBuilder.MoveToImmutable();
        _parameterMap = mapBuilder.ToFrozenDictionary();
    }
}
