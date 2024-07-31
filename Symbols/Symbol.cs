using Antlr4.Runtime;
using Ca21.Binding;

namespace Ca21.Symbols;

internal abstract class Symbol
{
    public static readonly Symbol Missing = new MissingSymbol();

    public abstract ParserRuleContext Context { get; }
    public abstract string Name { get; }
    public virtual TypeSymbol Type => TypeSymbol.BadType;
    public virtual Binder Binder => throw new InvalidOperationException();

    private sealed class MissingSymbol : Symbol
    {
        public override ParserRuleContext Context => ParserRuleContext.EMPTY;
        public override string Name => "???";
    }
}
