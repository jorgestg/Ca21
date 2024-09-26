using System.Collections.Immutable;
using Ca21.Diagnostics;
using Ca21.Symbols;
using static Ca21.Antlr.Ca21Parser;

namespace Ca21.Binding;

internal abstract class Binder
{
    public abstract Binder Parent { get; }

    public virtual Symbol? Lookup(string name)
    {
        return Parent.Lookup(name);
    }

    public virtual TypeSymbol BindType(TypeReferenceContext context, DiagnosticList diagnostics)
    {
        return Parent.BindType(context, diagnostics);
    }

    public BoundBlock BindBlock(BlockContext context, DiagnosticList diagnostics)
    {
        var localScopeBinder = new LocalScopeBinder(this);
        var statements = context
            ._Statements.Select(statement => localScopeBinder.BindStatement(statement, diagnostics))
            .ToImmutableArray();

        return new BoundBlock(context, statements);
    }

    public virtual TypeSymbol GetReturnType()
    {
        return Parent.GetReturnType();
    }

    protected static BoundExpression BindConversion(
        BoundExpression expression,
        TypeSymbol targetType,
        DiagnosticList diagnostics
    )
    {
        var unifiedType = TypeSymbol.Unify(targetType, expression.Type);
        if (unifiedType == targetType)
        {
            return expression.Type != targetType
                ? new BoundCastExpression(expression.Context, expression, targetType)
                : expression;
        }

        // An error should be already reported in these cases
        if (unifiedType != TypeSymbol.Missing)
            diagnostics.Add(expression.Context, DiagnosticMessages.TypeMismatch(targetType, expression.Type));

        return expression;
    }
}
