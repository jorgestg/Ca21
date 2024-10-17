using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Ca21.Diagnostics;
using static Ca21.Antlr.Ca21Parser;

namespace Ca21.Symbols;

internal sealed class EnumerationSymbol(EnumerationDefinitionContext context, ModuleSymbol module)
    : TypeSymbol,
        IContainingSymbol,
        IMemberSymbol
{
    public override EnumerationDefinitionContext Context { get; } = context;
    public override string Name => Context.Name.Text;
    public override TypeKind TypeKind => TypeKind.Enumeration;
    public IContainingSymbol ContainingSymbol { get; } = module;

    private ImmutableArray<Diagnostic> _diagnostics;
    public ImmutableArray<Diagnostic> Diagnostics
    {
        get
        {
            if (_diagnostics.IsDefault)
                CreateCases();

            return _diagnostics;
        }
    }

    private ImmutableArray<EnumerationCaseSymbol> _cases;
    public ImmutableArray<EnumerationCaseSymbol> Cases
    {
        get
        {
            if (_cases.IsDefault)
                CreateCases();

            return _cases;
        }
    }

    private FrozenDictionary<string, EnumerationCaseSymbol>? _caseMap;
    public FrozenDictionary<string, EnumerationCaseSymbol> CaseMap
    {
        get
        {
            if (_caseMap == null)
                CreateCases();

            return _caseMap;
        }
    }

    public override bool TryGetMember(string name, out IMemberSymbol member)
    {
        if (CaseMap.TryGetValue(name, out var @case))
        {
            member = @case;
            return true;
        }

        return base.TryGetMember(name, out member);
    }

    [MemberNotNull(nameof(_caseMap))]
    private void CreateCases()
    {
        DiagnosticList? diagnostics = null;
        var casesBuilder = new ArrayBuilder<EnumerationCaseSymbol>(Context._Cases.Count);
        var caseMapBuilder = new Dictionary<string, EnumerationCaseSymbol>();
        foreach (var caseContext in Context._Cases)
        {
            var caseSymbol = new EnumerationCaseSymbol(caseContext, this);
            casesBuilder.Add(caseSymbol);

            if (!caseMapBuilder.TryAdd(caseSymbol.Name, caseSymbol))
            {
                diagnostics ??= new DiagnosticList();
                diagnostics.Add(caseContext, DiagnosticMessages.NameIsAlreadyDefined(caseSymbol));
            }
        }

        _diagnostics = diagnostics?.GetImmutableArray() ?? [];
        _cases = casesBuilder.MoveToImmutable();
        _caseMap = caseMapBuilder.ToFrozenDictionary();
    }
}

internal sealed class EnumerationCaseSymbol(EnumerationCaseDefinitionContext context, EnumerationSymbol enumeration)
    : Symbol,
        IMemberSymbol
{
    public override SymbolKind SymbolKind => SymbolKind.EnumerationCase;
    public override EnumerationCaseDefinitionContext Context { get; } = context;
    public override string Name => Context.Name.Text;
    public override TypeSymbol Type => (EnumerationSymbol)ContainingSymbol;
    public IContainingSymbol ContainingSymbol { get; } = enumeration;
    public int Tag => ((EnumerationSymbol)ContainingSymbol).Context._Cases.IndexOf(Context);
}
