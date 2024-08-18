using System.Collections.Frozen;
using System.Collections.Immutable;
using Ca21.Binding;
using Ca21.Diagnostics;
using static Ca21.Antlr.Ca21Parser;

namespace Ca21.Symbols;

internal interface IModuleMemberSymbol
{
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

        var topLevelStructures = context._Definitions.OfType<TopLevelStructureDefinitionContext>();
        var structuresBuilder = new ArrayBuilder<StructureSymbol>(topLevelStructures.Count());
        foreach (var structureContext in topLevelStructures)
        {
            var structureSymbol = new StructureSymbol(structureContext.Structure, this);
            structuresBuilder.Add(structureSymbol);

            if (!memberMapBuilder.TryAdd(structureSymbol.Name, structureSymbol))
            {
                diagnosticsBuilder.Add(
                    structureContext.Structure.Name,
                    DiagnosticMessages.NameIsAlreadyDefined(structureSymbol.Name)
                );
            }
        }

        var topLevelFunctions = context._Definitions.OfType<TopLevelFunctionDefinitionContext>();
        var functionsBuilder = new ArrayBuilder<SourceFunctionSymbol>(topLevelFunctions.Count());
        foreach (var functionContext in topLevelFunctions)
        {
            var functionSymbol = new SourceFunctionSymbol(functionContext.Function, this);
            functionsBuilder.Add(functionSymbol);

            if (!memberMapBuilder.TryAdd(functionSymbol.Name, functionSymbol))
            {
                diagnosticsBuilder.Add(
                    functionContext.Function.Signature.Name,
                    DiagnosticMessages.NameIsAlreadyDefined(functionSymbol.Name)
                );
            }
        }

        Diagnostics = diagnosticsBuilder.GetImmutableArray();
        Structures = structuresBuilder.MoveToImmutable();
        Functions = functionsBuilder.MoveToImmutable();
        MemberMap = memberMapBuilder.ToFrozenDictionary();
    }

    public override CompilationUnitContext Context { get; }
    public override Binder Binder { get; }
    public override string Name { get; }
    public override ImmutableArray<Diagnostic> Diagnostics { get; }
    public ImmutableArray<StructureSymbol> Structures { get; }
    public ImmutableArray<SourceFunctionSymbol> Functions { get; }
    public FrozenDictionary<string, IModuleMemberSymbol> MemberMap { get; }
}
