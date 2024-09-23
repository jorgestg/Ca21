using System.Collections.Frozen;
using System.Collections.Immutable;
using Ca21.Binding;
using Ca21.Diagnostics;
using static Ca21.Antlr.Ca21Parser;

namespace Ca21.Symbols;

internal interface IModuleMemberSymbol
{
    SymbolKind Kind { get; }
    ModuleSymbol ContainingModule { get; }
}

internal sealed class ModuleSymbol : Symbol
{
    public ModuleSymbol(CompilationUnitContext context, string name)
    {
        Context = context;
        Name = name;
        Binder = new ModuleBinder(this);

        var diagnosticsBuilder = new DiagnosticList();
        var memberMapBuilder = new Dictionary<string, IModuleMemberSymbol>();
        var membersBuilder = new ArrayBuilder<IModuleMemberSymbol>(context._Definitions.Count);
        foreach (var definitionContext in context._Definitions)
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
                            DiagnosticMessages.NameIsAlreadyDefined(functionSymbol.Name)
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
                            DiagnosticMessages.NameIsAlreadyDefined(structureSymbol.Name)
                        );
                    }

                    break;
                }
            }
        }

        Diagnostics = diagnosticsBuilder.GetImmutableArray();
        Members = membersBuilder.MoveToImmutable();
        MemberMap = memberMapBuilder.ToFrozenDictionary();
    }

    public override SymbolKind Kind => SymbolKind.Module;
    public override CompilationUnitContext Context { get; }
    public override string Name { get; }
    public ModuleBinder Binder { get; }
    public ImmutableArray<Diagnostic> Diagnostics { get; }
    public ImmutableArray<IModuleMemberSymbol> Members { get; }
    public FrozenDictionary<string, IModuleMemberSymbol> MemberMap { get; }

    public IEnumerable<T> GetMembers<T>()
        where T : IModuleMemberSymbol
    {
        foreach (var member in Members)
        {
            if (member is T t)
                yield return t;
        }
    }
}
