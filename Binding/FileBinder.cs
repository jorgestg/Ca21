using System.Collections.Frozen;
using Ca21.Symbols;
using static Ca21.Antlr.Ca21Parser;

namespace Ca21.Binding;

internal sealed class FileBinder(ModuleBinder parent, CompilationUnitContext root) : Binder
{
    public override ModuleBinder Parent { get; } = parent;
    public CompilationUnitContext Root { get; } = root;

    private FrozenDictionary<string, Symbol>? _symbols;
    private FrozenDictionary<string, Symbol> Symbols => _symbols ??= InitializeSymbols();

    private FrozenDictionary<string, Symbol> InitializeSymbols()
    {
        var symbols = new Dictionary<string, Symbol>();
        var imports = Parent.ModuleSymbol.Imports[Root];
        foreach (var import in imports)
            symbols.Add(import.Alias ?? import.ModuleSymbol.Name, import.ModuleSymbol);

        return symbols.ToFrozenDictionary();
    }

    public override Symbol? Lookup(string name) => Symbols.GetValueOrDefault(name) ?? Parent.Lookup(name);
}
