using Ca21.Diagnostics;
using Ca21.Symbols;

namespace Ca21.Binding;

internal sealed class FunctionBinder(SourceFunctionSymbol functionSymbol) : Binder
{
    public override Binder Parent => throw new InvalidOperationException();
    public SourceFunctionSymbol FunctionSymbol { get; } = functionSymbol;

    public BoundBlock BindBody(DiagnosticList diagnostics)
    {
        var boundBody = BindBlock(FunctionSymbol.Context.Body, diagnostics);
        var cfg = ControlFlowGraph.Create(boundBody);
        if (!cfg.AllPathsReturn() && FunctionSymbol.ReturnType != TypeSymbol.Unit)
        {
            diagnostics.Add(FunctionSymbol.Context.Name, DiagnosticMessages.AllCodePathsMustReturn);
        }

        return boundBody;
    }

    public override Symbol? Lookup(string name)
    {
        return null;
    }

    public override TypeSymbol GetReturnType()
    {
        return FunctionSymbol.ReturnType;
    }
}
