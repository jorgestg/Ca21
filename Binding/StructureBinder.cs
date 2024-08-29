using Ca21.Symbols;

namespace Ca21.Binding;

internal sealed class StructureBinder(StructureSymbol structureSymbol) : Binder
{
    private readonly StructureSymbol _structureSymbol = structureSymbol;

    public override Binder Parent => _structureSymbol.ContainingModule.Binder;

    public override Symbol? Lookup(string name) =>
        _structureSymbol.FieldMap.GetValueOrDefault(name) ?? base.Lookup(name);
}
