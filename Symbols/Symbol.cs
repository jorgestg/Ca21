using Antlr4.Runtime;

namespace Ca21.Symbols;

internal enum SymbolKind
{
    None,
    Module,
    Type,
    Field,
    Function,
    Local
}

internal abstract class Symbol
{
    public static readonly Symbol Missing = new MissingSymbol();

    public abstract SymbolKind Kind { get; }
    public abstract ParserRuleContext Context { get; }
    public abstract string Name { get; }
    public virtual TypeSymbol Type => TypeSymbol.Missing;

    private sealed class MissingSymbol : Symbol
    {
        public override SymbolKind Kind => SymbolKind.None;
        public override ParserRuleContext Context => ParserRuleContext.EMPTY;
        public override string Name => "???";
    }
}
