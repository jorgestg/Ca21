using System.Collections.Immutable;
using Antlr4.Runtime;
using Ca21.Symbols;

namespace Ca21.Binding;

internal abstract class BoundExpression(ParserRuleContext context)
{
    public ParserRuleContext Context { get; } = context;
    public abstract TypeSymbol Type { get; }
    public virtual BoundConstant ConstantValue => default;
}

internal sealed class BoundBlockExpression(
    ParserRuleContext context,
    ImmutableArray<BoundStatement> statements,
    BoundExpression? tailExpression
) : BoundExpression(context)
{
    public override TypeSymbol Type => TailExpression?.Type ?? TypeSymbol.Unit;
    public ImmutableArray<BoundStatement> Statements { get; } = statements;
    public BoundExpression? TailExpression { get; } = tailExpression;
}

internal sealed class BoundLiteralExpression(ParserRuleContext context, object value, TypeSymbol type)
    : BoundExpression(context)
{
    public override TypeSymbol Type { get; } = type;
    public override BoundConstant ConstantValue { get; } = new(value);
    public object Value { get; } = value;
}

internal sealed class BoundCallExpression(
    ParserRuleContext context,
    FunctionSymbol function,
    ImmutableArray<BoundExpression> arguments
) : BoundExpression(context)
{
    public override TypeSymbol Type => Function.ReturnType;
    public FunctionSymbol Function { get; } = function;
    public ImmutableArray<BoundExpression> Arguments { get; } = arguments;
}

internal sealed class BoundNameExpression(ParserRuleContext context, Symbol referencedSymbol) : BoundExpression(context)
{
    public override TypeSymbol Type => ReferencedSymbol.Type;
    public Symbol ReferencedSymbol { get; } = referencedSymbol;
}

internal sealed class BoundBinaryExpression : BoundExpression
{
    public BoundBinaryExpression(
        ParserRuleContext context,
        BoundExpression left,
        BoundBinaryOperator @operator,
        BoundExpression right
    )
        : base(context)
    {
        Left = left;
        Operator = @operator;
        Right = right;
        ConstantValue = ConstantFolding.Fold(this);
    }

    public override TypeSymbol Type => Operator.ResultType;
    public override BoundConstant ConstantValue { get; }
    public BoundExpression Left { get; }
    public BoundBinaryOperator Operator { get; }
    public BoundExpression Right { get; }
}

internal sealed class BoundAssignmentExpression(ParserRuleContext context, Symbol assignee, BoundExpression value)
    : BoundExpression(context)
{
    public override TypeSymbol Type => TypeSymbol.Unit;
    public Symbol Assignee { get; } = assignee;
    public BoundExpression Value { get; } = value;
}
