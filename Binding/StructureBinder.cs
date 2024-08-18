using Ca21.Symbols;

namespace Ca21.Binding;

internal sealed class StructureBinder(StructureSymbol structureSymbol) : Binder
{
    private readonly StructureSymbol _structureSymbol = structureSymbol;

    public override Binder Parent => _structureSymbol.Binder;

    public override Symbol? Lookup(string name)
    {
        return _structureSymbol.FieldMap.GetValueOrDefault(name) ?? base.Lookup(name);
    }
}
