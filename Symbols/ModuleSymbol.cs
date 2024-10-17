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
    public ModuleSymbol(PackageSymbol package, string name, ImmutableArray<CompilationUnitContext> roots)
    {
        Package = package;
        Name = name;
        Roots = roots;
        Binder = new ModuleBinder(this);
    }

    public override SymbolKind SymbolKind => SymbolKind.Module;
    public override CompilationUnitContext Context => throw new InvalidOperationException();

    public override string Name { get; }

    public ImmutableArray<CompilationUnitContext> Roots { get; }
    public ModuleBinder Binder { get; }

    public PackageSymbol Package { get; }

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

    private FrozenDictionary<CompilationUnitContext, ImmutableArray<ModuleSymbol>>? _imports;
    public FrozenDictionary<CompilationUnitContext, ImmutableArray<ModuleSymbol>> Imports
    {
        get
        {
            if (_imports == null)
                InitializeProperties();

            return _imports;
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

    [MemberNotNull(nameof(_memberMap), nameof(_imports))]
    private void InitializeProperties()
    {
        var diagnosticsBuilder = new DiagnosticList();

        var definitionCount = 0;
        foreach (var root in Roots)
            definitionCount += root._Definitions.Count;

        var importsMapBuilder = new Dictionary<CompilationUnitContext, List<ModuleSymbol>>(Roots.Length);
        var memberMapBuilder = new Dictionary<string, IModuleMemberSymbol>(definitionCount);
        var membersBuilder = new ArrayBuilder<IModuleMemberSymbol>(definitionCount);
        foreach (var root in Roots)
        {
            CreateDefinition(diagnosticsBuilder, memberMapBuilder, ref membersBuilder, importsMapBuilder, root);
        }

        _diagnostics = diagnosticsBuilder.GetImmutableArray();
        _imports = importsMapBuilder.ToFrozenDictionary(pair => pair.Key, kvp => kvp.Value.ToImmutableArray());
        _members = membersBuilder.MoveToImmutable();
        _memberMap = memberMapBuilder.ToFrozenDictionary();
    }

    private void CreateDefinition(
        DiagnosticList diagnostics,
        Dictionary<string, IModuleMemberSymbol> memberMap,
        ref ArrayBuilder<IModuleMemberSymbol> members,
        Dictionary<CompilationUnitContext, List<ModuleSymbol>> importMap,
        CompilationUnitContext root
    )
    {
        foreach (var context in root._UseDirectives)
        {
            var path = context.Path.Text.Trim('"');
            var module = Package.ModuleMap.GetValueOrDefault(path);
            if (module == null)
            {
                diagnostics.Add(context.Path, DiagnosticMessages.ModuleNotFound(path));
                continue;
            }

            if (!importMap.TryGetValue(root, out var imports))
            {
                imports = [];
                importMap.Add(root, imports);
            }

            imports.Add(module);
        }

        foreach (var definitionContext in root._Definitions)
        {
            switch (definitionContext)
            {
                case TopLevelFunctionDefinitionContext { Function: var functionContext }:
                {
                    var functionSymbol = new SourceFunctionSymbol(functionContext, this);
                    members.Add(functionSymbol);

                    if (!memberMap.TryAdd(functionSymbol.Name, functionSymbol))
                    {
                        diagnostics.Add(
                            functionContext.Signature.Name,
                            DiagnosticMessages.NameIsAlreadyDefined(functionSymbol)
                        );
                    }

                    break;
                }
                case TopLevelStructureDefinitionContext { Structure: var structureContext }:
                {
                    var structureSymbol = new StructureSymbol(structureContext, this);
                    members.Add(structureSymbol);

                    if (!memberMap.TryAdd(structureSymbol.Name, structureSymbol))
                    {
                        diagnostics.Add(
                            structureContext.Name,
                            DiagnosticMessages.NameIsAlreadyDefined(structureSymbol)
                        );
                    }

                    break;
                }
                case TopLevelEnumerationDefinitionContext { Enumeration: var enumerationContext }:
                {
                    var enumerationSymbol = new EnumerationSymbol(enumerationContext, this);
                    members.Add(enumerationSymbol);

                    if (!memberMap.TryAdd(enumerationSymbol.Name, enumerationSymbol))
                    {
                        diagnostics.Add(
                            enumerationContext.Name,
                            DiagnosticMessages.NameIsAlreadyDefined(enumerationSymbol)
                        );
                    }

                    break;
                }
            }
        }
    }
}
