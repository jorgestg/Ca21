using Ca21.Diagnostics;
using Ca21.Symbols;

namespace Ca21.Binding;

internal sealed class FunctionBinder(SourceFunctionSymbol functionSymbol) : Binder
{
    public override Binder Parent => throw new InvalidOperationException();
    public SourceFunctionSymbol FunctionSymbol { get; } = functionSymbol;

    public BoundBlock BindBody(DiagnosticList diagnostics) => BindBlock(FunctionSymbol.Context.Body, diagnostics);

    public override Symbol? Lookup(string name)
    {
        return null;
    }

    public override TypeSymbol GetReturnType()
    {
        return FunctionSymbol.ReturnType;
    }
}
