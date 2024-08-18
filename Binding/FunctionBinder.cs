using Ca21.Diagnostics;
using Ca21.Symbols;

namespace Ca21.Binding;

internal sealed class FunctionBinder(SourceFunctionSymbol functionSymbol) : Binder
{
    public readonly SourceFunctionSymbol _functionSymbol = functionSymbol;

    public override Binder Parent => _functionSymbol.ContainingModule.Binder;

    public override Symbol? Lookup(string name)
    {
        return _functionSymbol.ParameterMap.GetValueOrDefault(name) ?? Parent.Lookup(name);
    }

    public BoundBlock BindBody(DiagnosticList diagnostics)
    {
        var context = _functionSymbol.Context;
        if (context.Body == null)
        {
            if (!_functionSymbol.IsExtern)
            {
                diagnostics.Add(
                    context.EndOfDeclaration ?? context.Signature.Name,
                    DiagnosticMessages.FunctionMustHaveABody
                );
            }

            return new BoundBlock(context, []);
        }

        if (_functionSymbol.IsExtern)
            diagnostics.Add(context.Body, DiagnosticMessages.FunctionMustNotHaveABody);

        var boundBody = BindBlock(context.Body, diagnostics);
        var loweredBody = Lowerer.Lower(boundBody);
        if (_functionSymbol.ReturnType == TypeSymbol.Unit)
            return loweredBody;

        var cfg = ControlFlowGraph.Create(loweredBody);
        if (!cfg.AllPathsReturn())
            diagnostics.Add(context.Signature.Name, DiagnosticMessages.AllCodePathsMustReturn);

        return loweredBody;
    }

    public override TypeSymbol GetReturnType() => _functionSymbol.ReturnType;
}
