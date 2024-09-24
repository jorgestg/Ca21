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

    public ControlFlowGraph? BindBody(DiagnosticList diagnostics)
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

            return null;
        }

        if (_functionSymbol.IsExtern)
            diagnostics.Add(context.Body, DiagnosticMessages.FunctionMustNotHaveABody);

        var boundBody = BindBlock(context.Body, diagnostics);
        var loweredBody = Lowerer.Lower(boundBody);
        var cfg = ControlFlowGraph.Create(loweredBody);
        if (_functionSymbol.ReturnType != TypeSymbol.Unit && !cfg.AllPathsReturn())
            diagnostics.Add(_functionSymbol.Context.Signature.Name, DiagnosticMessages.AllCodePathsMustReturn);

        var unreachableStatements = cfg.GetUnreachableStatements();
        if (unreachableStatements.Length == 0)
            return cfg;

        foreach (var unreachableStatement in unreachableStatements)
            diagnostics.Add(unreachableStatement.Context, DiagnosticMessages.CodeIsUnreachable);

        return cfg;
    }

    public override TypeSymbol GetReturnType() => _functionSymbol.ReturnType;
}
