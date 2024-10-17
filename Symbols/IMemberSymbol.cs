namespace Ca21.Symbols;

internal interface IMemberSymbol : ISymbol
{
    IContainingSymbol ContainingSymbol { get; }
}

internal interface IContainingSymbol : ISymbol
{
    bool TryGetMember(string name, out IMemberSymbol symbol);
}
