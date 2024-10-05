using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Ca21.Binding;
using Ca21.Diagnostics;
using static Ca21.Antlr.Ca21Parser;

namespace Ca21.Symbols;

internal interface IModuleMemberSymbol : ISymbol
{
    ModuleSymbol ContainingModule { get; }
}

internal sealed class ModuleSymbol : Symbol
{
    public ModuleSymbol(ImmutableArray<CompilationUnitContext> roots, string name)
    {
        Roots = roots;
        Name = name;
        Binder = new ModuleBinder(this);
    }

    public override SymbolKind SymbolKind => SymbolKind.Module;
    public override CompilationUnitContext Context => throw new InvalidOperationException();
    public override string Name { get; }

    public ImmutableArray<CompilationUnitContext> Roots { get; }
    public ModuleBinder Binder { get; }

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

    private ImmutableArray<IModuleMemberSymbol> _members;
    public ImmutableArray<IModuleMemberSymbol> Members
    {
        get
        {
            if (_members.IsDefault)
                InitializeProperties();

            return _members;
        }
    }

    private FrozenDictionary<string, IModuleMemberSymbol>? _memberMap;
    public FrozenDictionary<string, IModuleMemberSymbol> MemberMap
    {
        get
        {
            if (_memberMap == null)
                InitializeProperties();

            return _memberMap;
        }
    }

    public IEnumerable<T> GetMembers<T>()
        where T : IModuleMemberSymbol
    {
        foreach (var member in Members)
        {
            if (member is T t)
                yield return t;
        }
    }

    [MemberNotNull(nameof(_memberMap))]
    private void InitializeProperties()
    {
        var diagnosticsBuilder = new DiagnosticList();

        var definitionCount = 0;
        foreach (var root in Roots)
            definitionCount += root._Definitions.Count;

        var memberMapBuilder = new Dictionary<string, IModuleMemberSymbol>(definitionCount);
        var membersBuilder = new ArrayBuilder<IModuleMemberSymbol>(definitionCount);
        foreach (var root in Roots)
        {
            foreach (var definitionContext in root._Definitions)
            {
                switch (definitionContext)
                {
                    case TopLevelFunctionDefinitionContext { Function: var functionContext }:
                    {
                        var functionSymbol = new SourceFunctionSymbol(functionContext, this);
                        membersBuilder.Add(functionSymbol);

                        if (!memberMapBuilder.TryAdd(functionSymbol.Name, functionSymbol))
                        {
                            diagnosticsBuilder.Add(
                                functionContext.Signature.Name,
                                DiagnosticMessages.NameIsAlreadyDefined(functionSymbol)
                            );
                        }

                        break;
                    }
                    case TopLevelStructureDefinitionContext { Structure: var structureContext }:
                    {
                        var structureSymbol = new StructureSymbol(structureContext, this);
                        membersBuilder.Add(structureSymbol);

                        if (!memberMapBuilder.TryAdd(structureSymbol.Name, structureSymbol))
                        {
                            diagnosticsBuilder.Add(
                                structureContext.Name,
                                DiagnosticMessages.NameIsAlreadyDefined(structureSymbol)
                            );
                        }

                        break;
                    }
                    case TopLevelEnumerationDefinitionContext { Enumeration: var enumerationContext }:
                    {
                        var enumerationSymbol = new EnumerationSymbol(enumerationContext, this);
                        membersBuilder.Add(enumerationSymbol);

                        if (!memberMapBuilder.TryAdd(enumerationSymbol.Name, enumerationSymbol))
                        {
                            diagnosticsBuilder.Add(
                                enumerationContext.Name,
                                DiagnosticMessages.NameIsAlreadyDefined(enumerationSymbol)
                            );
                        }

                        break;
                    }
                }
            }
        }

        _diagnostics = diagnosticsBuilder.GetImmutableArray();
        _members = membersBuilder.MoveToImmutable();
        _memberMap = memberMapBuilder.ToFrozenDictionary();
    }
}
