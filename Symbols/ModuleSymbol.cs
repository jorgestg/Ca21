using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using Ca21.Binding;
using Ca21.Diagnostics;
using static Ca21.Antlr.Ca21Parser;

namespace Ca21.Symbols;

internal readonly struct ModuleImport(ModuleSymbol moduleSymbol, string? alias)
{
    public ModuleSymbol ModuleSymbol { get; } = moduleSymbol;
    public string? Alias { get; } = alias;
}

internal sealed class ModuleSymbol : Symbol, IContainingSymbol
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

    private FrozenDictionary<CompilationUnitContext, ImmutableArray<ModuleImport>>? _imports;
    public FrozenDictionary<CompilationUnitContext, ImmutableArray<ModuleImport>> Imports
    {
        get
        {
            if (_imports == null)
                InitializeProperties();

            return _imports;
        }
    }

    private ImmutableArray<IMemberSymbol> _members;
    public ImmutableArray<IMemberSymbol> Members
    {
        get
        {
            if (_members.IsDefault)
                InitializeProperties();

            return _members;
        }
    }

    private FrozenDictionary<string, IMemberSymbol>? _memberMap;
    public FrozenDictionary<string, IMemberSymbol> MemberMap
    {
        get
        {
            if (_memberMap == null)
                InitializeProperties();

            return _memberMap;
        }
    }

    public bool TryGetMember(string name, out IMemberSymbol symbol)
    {
        var result = MemberMap.TryGetValue(name, out var member);
        symbol = member ?? MemberSymbol.Missing;
        return result;
    }

    public IEnumerable<T> GetMembers<T>()
        where T : IMemberSymbol
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

        var importsMapBuilder = new Dictionary<CompilationUnitContext, List<ModuleImport>>(Roots.Length);
        var memberMapBuilder = new Dictionary<string, IMemberSymbol>(definitionCount);
        var membersBuilder = new ArrayBuilder<IMemberSymbol>(definitionCount);
        foreach (var root in Roots)
        {
            CreateDefinition(diagnosticsBuilder, memberMapBuilder, ref membersBuilder, importsMapBuilder, root);
        }

        _diagnostics = diagnosticsBuilder.GetImmutableArray();
        _imports = importsMapBuilder.ToFrozenDictionary(pair => pair.Key, pair => pair.Value.ToImmutableArray());
        _members = membersBuilder.MoveToImmutable();
        _memberMap = memberMapBuilder.ToFrozenDictionary();
    }

    private void CreateDefinition(
        DiagnosticList diagnostics,
        Dictionary<string, IMemberSymbol> memberMap,
        ref ArrayBuilder<IMemberSymbol> members,
        Dictionary<CompilationUnitContext, List<ModuleImport>> importMap,
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

            if (context.Alias == null && IsInvalidIdentifier(module.Name))
            {
                diagnostics.Add(context.Path, DiagnosticMessages.ModuleNameIsNotAnIdentifier(module.Name));
                continue;
            }

            imports.Add(new ModuleImport(module, context.Alias?.Text));
        }

        foreach (var definitionContext in root._Definitions)
        {
            switch (definitionContext)
            {
                case TopLevelFunctionDefinitionContext { Function: var functionContext }:
                {
                    var functionSymbol = new SourceFunctionSymbol(functionContext, this, Binder.GetFileBinder(root));
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
                    var structureSymbol = new StructureSymbol(structureContext, this, Binder.GetFileBinder(root));
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

    private static bool IsInvalidIdentifier(string name)
    {
        var c = name[0];
        if (!char.IsAsciiLetter(c) && c != '_')
            return true;

        for (var i = 1; i < name.Length; i++)
        {
            c = name[i];
            if (!char.IsAsciiLetterOrDigit(c) && c != '_')
                return true;
        }

        return false;
    }
}
