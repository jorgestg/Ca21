using Antlr4.Runtime;

namespace Ca21.Symbols;

internal abstract class Symbol
{
    public static readonly Symbol Missing = new MissingSymbol();

    public abstract ParserRuleContext Context { get; }
    public abstract string Name { get; }
    public virtual TypeSymbol Type => TypeSymbol.Missing;

    private sealed class MissingSymbol : Symbol
    {
        public override ParserRuleContext Context => ParserRuleContext.EMPTY;
        public override string Name => "???";
    }
}
