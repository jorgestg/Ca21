using System.Collections.Immutable;
using Antlr4.Runtime;
using Ca21.Symbols;

namespace Ca21.Binding;

internal abstract class BoundExpression(ParserRuleContext context)
{
    public ParserRuleContext Context { get; } = context;
    public abstract TypeSymbol Type { get; }
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

internal sealed class BoundLiteral(ParserRuleContext context, object value, TypeSymbol type) : BoundExpression(context)
{
    public override TypeSymbol Type { get; } = type;
    public object Value { get; } = value;
}

internal sealed class BoundNameExpression(ParserRuleContext context, Symbol referencedSymbol) : BoundExpression(context)
{
    public override TypeSymbol Type => ReferencedSymbol.Type;
    public Symbol ReferencedSymbol { get; } = referencedSymbol;
}

internal sealed class BoundBinaryExpression(
    ParserRuleContext context,
    BoundExpression left,
    BoundBinaryOperator @operator,
    BoundExpression right
) : BoundExpression(context)
{
    public override TypeSymbol Type => Operator.ResultType;
    public BoundExpression Left { get; } = left;
    public BoundBinaryOperator Operator { get; } = @operator;
    public BoundExpression Right { get; } = right;
}

internal sealed class BoundAssignmentExpression(ParserRuleContext context, Symbol assignee, BoundExpression value)
    : BoundExpression(context)
{
    public override TypeSymbol Type => TypeSymbol.Unit;
    public Symbol Assignee { get; } = assignee;
    public BoundExpression Value { get; } = value;
}
