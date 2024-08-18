using Ca21.Symbols;

namespace Ca21.Binding;

internal sealed class ModuleBinder(ModuleSymbol moduleSymbol) : Binder
{
    private readonly ModuleSymbol _moduleSymbol = moduleSymbol;

    public override Binder Parent => throw new InvalidOperationException();

    public override Symbol? Lookup(string name) => (Symbol?)_moduleSymbol.MemberMap.GetValueOrDefault(name);
}
