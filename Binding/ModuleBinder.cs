using Ca21.Symbols;

namespace Ca21.Binding;

internal sealed class ModuleBinder : Binder
{
    public override Binder Parent => throw new InvalidOperationException();

    public override Symbol? Lookup(string name)
    {
        return null;
    }
}
