using Ca21.Diagnostics;
using Ca21.Symbols;
using static Ca21.Antlr.Ca21Parser;

namespace Ca21.Binding;

internal sealed class FunctionBinder(SourceFunctionSymbol functionSymbol) : Binder
{
    public override Binder Parent => FunctionSymbol.Module.Binder;
    public SourceFunctionSymbol FunctionSymbol { get; } = functionSymbol;

    public override Symbol? Lookup(string name)
    {
        return FunctionSymbol.ParameterMap.GetValueOrDefault(name) ?? Parent.Lookup(name);
    }

    public BoundBlock BindBody(DiagnosticList diagnostics)
    {
        var context = (TopLevelFunctionDefinitionContext)FunctionSymbol.Context;
        var boundBody = BindBlock(context.Body, diagnostics);
        var loweredBody = Lowerer.Lower(boundBody);
        if (FunctionSymbol.ReturnType == TypeSymbol.Unit)
            return loweredBody;

        var cfg = ControlFlowGraph.Create(loweredBody);
        if (!cfg.AllPathsReturn())
            diagnostics.Add(context.Signature.Name, DiagnosticMessages.AllCodePathsMustReturn);

        return loweredBody;
    }

    public override TypeSymbol GetReturnType()
    {
        return FunctionSymbol.ReturnType;
    }
}
