using System.Collections.Immutable;
using Antlr4.Runtime;
using Ca21.Symbols;

namespace Ca21.Binding;

internal abstract class BoundStatement(ParserRuleContext context)
{
    public ParserRuleContext Context { get; } = context;
}

internal sealed class BoundLabelStatement(ParserRuleContext context, LabelSymbol target) : BoundStatement(context)
{
    public LabelSymbol Label { get; } = target;
}

internal sealed class BoundGotoStatement(ParserRuleContext context, LabelSymbol target) : BoundStatement(context)
{
    public LabelSymbol Target { get; } = target;
}

internal sealed class BoundConditionalGotoStatement(
    ParserRuleContext context,
    BoundExpression condition,
    LabelSymbol target
) : BoundStatement(context)
{
    public BoundExpression Condition { get; } = condition;
    public LabelSymbol Target { get; } = target;
}

internal sealed class BoundLocalDeclaration(
    ParserRuleContext context,
    SourceLocalSymbol local,
    BoundExpression initializer
) : BoundStatement(context)
{
    public SourceLocalSymbol Local { get; } = local;
    public BoundExpression Initializer { get; } = initializer;
}

internal sealed class BoundWhileStatement(
    ParserRuleContext context,
    BoundExpression condition,
    BoundBlock body,
    LabelSymbol continueLabel,
    LabelSymbol breakLabel
) : BoundStatement(context)
{
    public BoundExpression Condition { get; } = condition;
    public BoundBlock Body { get; } = body;
    public LabelSymbol ContinueLabel { get; } = continueLabel;
    public LabelSymbol BreakLabel { get; } = breakLabel;
}

internal sealed class BoundReturnStatement(ParserRuleContext context, BoundExpression value) : BoundStatement(context)
{
    public BoundExpression Value { get; } = value;
}

internal sealed class BoundBlock(ParserRuleContext context, ImmutableArray<BoundStatement> statements)
    : BoundStatement(context)
{
    public ImmutableArray<BoundStatement> Statements { get; } = statements;
}

internal sealed class BoundExpressionStatement(ParserRuleContext context, BoundExpression expression)
    : BoundStatement(context)
{
    public BoundExpression Expression { get; } = expression;
}
