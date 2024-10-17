using Ca21.Symbols;

namespace Ca21.Binding;

internal sealed class StructureBinder(Binder parent, StructureSymbol structureSymbol) : Binder
{
    private readonly StructureSymbol _structureSymbol = structureSymbol;

    public override Binder Parent { get; } = parent;

    public override Symbol? Lookup(string name) =>
        _structureSymbol.FieldMap.GetValueOrDefault(name) ?? base.Lookup(name);
}
