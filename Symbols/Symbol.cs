using Antlr4.Runtime;

namespace Ca21.Symbols;

internal enum SymbolKind
{
    None,
    Module,
    Function,
    Type,
    Field,
    EnumerationCase,
    Local
}

internal interface ISymbol
{
    SymbolKind SymbolKind { get; }
    string Name { get; }
    TypeSymbol Type { get; }
}

internal abstract class Symbol : ISymbol
{
    public static readonly Symbol Missing = new MissingSymbol();

    public abstract SymbolKind SymbolKind { get; }
    public abstract ParserRuleContext Context { get; }
    public abstract string Name { get; }
    public virtual TypeSymbol Type => TypeSymbol.Missing;

    private sealed class MissingSymbol : Symbol
    {
        public override SymbolKind SymbolKind => SymbolKind.None;
        public override ParserRuleContext Context => throw new InvalidOperationException();
        public override string Name => throw new InvalidOperationException();
    }
}
