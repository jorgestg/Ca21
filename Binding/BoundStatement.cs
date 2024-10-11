using System.Collections.Immutable;
using Antlr4.Runtime;
using Ca21.Symbols;

namespace Ca21.Binding;

internal abstract class BoundStatement(ParserRuleContext context) : BoundNode
{
    public ParserRuleContext Context { get; } = context;
}

internal sealed class BoundNopStatement(ParserRuleContext context) : BoundStatement(context)
{
    public override BoundNodeKind Kind => BoundNodeKind.NopStatement;
}

internal sealed class BoundLabelStatement(ParserRuleContext context, LabelSymbol label) : BoundStatement(context)
{
    public override BoundNodeKind Kind => BoundNodeKind.LabelStatement;
    public LabelSymbol Label { get; } = label;
}

internal sealed class BoundGotoStatement(ParserRuleContext context, LabelSymbol target) : BoundStatement(context)
{
    public override BoundNodeKind Kind => BoundNodeKind.GotoStatement;
    public LabelSymbol Target { get; } = target;
}

internal sealed class BoundConditionalGotoStatement(
    ParserRuleContext context,
    BoundExpression condition,
    LabelSymbol taget,
    bool branchIfFalse = false
) : BoundStatement(context)
{
    public override BoundNodeKind Kind => BoundNodeKind.ConditionalGotoStatement;
    public BoundExpression Condition { get; } = condition;
    public LabelSymbol Target { get; } = taget;
    public bool BranchIfFalse { get; } = branchIfFalse;
}

internal sealed class BoundLocalDeclaration(ParserRuleContext context, LocalSymbol local, BoundExpression? initializer)
    : BoundStatement(context)
{
    public override BoundNodeKind Kind => BoundNodeKind.LocalDeclaration;
    public LocalSymbol Local { get; } = local;
    public BoundExpression? Initializer { get; } = initializer;
}

internal sealed class BoundIfStatement(
    ParserRuleContext context,
    BoundExpression condition,
    BoundBlock body,
    BoundBlock? elseClause
) : BoundStatement(context)
{
    public override BoundNodeKind Kind => BoundNodeKind.IfStatement;
    public BoundExpression Condition { get; } = condition;
    public BoundBlock Body { get; } = body;
    public BoundBlock? ElseClause { get; } = elseClause;
}

internal sealed class BoundWhileStatement(
    ParserRuleContext context,
    BoundExpression condition,
    BoundBlock body,
    LabelSymbol continueIdentifier,
    LabelSymbol breakIdentifier
) : BoundStatement(context)
{
    public override BoundNodeKind Kind => BoundNodeKind.WhileStatement;
    public BoundExpression Condition { get; } = condition;
    public BoundBlock Body { get; } = body;
    public LabelSymbol ContinueLabel { get; } = continueIdentifier;
    public LabelSymbol BreakLabel { get; } = breakIdentifier;
}

internal sealed class BoundReturnStatement(ParserRuleContext context, BoundExpression? expression)
    : BoundStatement(context)
{
    public override BoundNodeKind Kind => BoundNodeKind.ReturnStatement;
    public BoundExpression? Expression { get; } = expression;
}

internal sealed class BoundBlock(ParserRuleContext context, ImmutableArray<BoundStatement> statements)
    : BoundStatement(context)
{
    public override BoundNodeKind Kind => BoundNodeKind.Block;
    public ImmutableArray<BoundStatement> Statements { get; } = statements;
}

internal sealed class BoundExpressionStatement(ParserRuleContext context, BoundExpression expression)
    : BoundStatement(context)
{
    public override BoundNodeKind Kind => BoundNodeKind.ExpressionStatement;
    public BoundExpression Expression { get; } = expression;
}
