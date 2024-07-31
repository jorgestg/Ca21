using Ca21.Diagnostics;
using Ca21.Symbols;

namespace Ca21.Binding;

internal sealed class FunctionBinder(SourceFunctionSymbol functionSymbol) : Binder
{
    public override Binder Parent => FunctionSymbol.Module.Binder;
    public SourceFunctionSymbol FunctionSymbol { get; } = functionSymbol;

    public BoundBlock BindBody(DiagnosticList diagnostics)
    {
        var boundBody = BindBlock(FunctionSymbol.Context.Body, diagnostics);
        var loweredBody = Lowerer.Lower(boundBody);
        if (FunctionSymbol.ReturnType == TypeSymbol.Unit)
            return loweredBody;

        var cfg = ControlFlowGraph.Create(loweredBody);
        if (!cfg.AllPathsReturn())
            diagnostics.Add(FunctionSymbol.Context.Name, DiagnosticMessages.AllCodePathsMustReturn);

        return loweredBody;
    }

    public override TypeSymbol GetReturnType()
    {
        return FunctionSymbol.ReturnType;
    }
}
