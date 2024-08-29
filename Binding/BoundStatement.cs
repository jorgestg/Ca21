using System.Collections.Immutable;
using Antlr4.Runtime;
using Ca21.Symbols;

namespace Ca21.Binding;

internal abstract class BoundStatement(ParserRuleContext context) : BoundNode
{
    public ParserRuleContext Context { get; } = context;
}

internal sealed class BoundLabel(string name)
{
    public string Name { get; } = name;
}

internal sealed class BoundNopStatement(ParserRuleContext context) : BoundStatement(context)
{
    public override BoundNodeKind Kind => BoundNodeKind.NopStatement;
}

internal sealed class BoundLabelStatement(ParserRuleContext context, BoundLabel label) : BoundStatement(context)
{
    public override BoundNodeKind Kind => BoundNodeKind.LabelStatement;
    public BoundLabel Label { get; } = label;
}

internal sealed class BoundGotoStatement(ParserRuleContext context, BoundLabel target) : BoundStatement(context)
{
    public override BoundNodeKind Kind => BoundNodeKind.GotoStatement;
    public BoundLabel Target { get; } = target;
}

internal sealed class BoundConditionalGotoStatement(
    ParserRuleContext context,
    BoundExpression condition,
    BoundLabel taget,
    bool branchIfFalse = false
) : BoundStatement(context)
{
    public override BoundNodeKind Kind => BoundNodeKind.ConditionalGotoStatement;
    public BoundExpression Condition { get; } = condition;
    public BoundLabel Target { get; } = taget;
    public bool BranchIfFalse { get; } = branchIfFalse;
}

internal sealed class BoundLocalDeclaration(ParserRuleContext context, LocalSymbol local, BoundExpression? initializer)
    : BoundStatement(context)
{
    public override BoundNodeKind Kind => BoundNodeKind.LocalDeclaration;
    public LocalSymbol Local { get; } = local;
    public BoundExpression? Initializer { get; } = initializer;
}

internal sealed class BoundWhileStatement(
    ParserRuleContext context,
    BoundExpression condition,
    BoundBlock body,
    BoundLabel continueIdentifier,
    BoundLabel breakIdentifier
) : BoundStatement(context)
{
    public override BoundNodeKind Kind => BoundNodeKind.WhileStatement;
    public BoundExpression Condition { get; } = condition;
    public BoundBlock Body { get; } = body;
    public BoundLabel ContinueLabel { get; } = continueIdentifier;
    public BoundLabel BreakLabel { get; } = breakIdentifier;
}

internal sealed class BoundReturnStatement(ParserRuleContext context, BoundExpression? value) : BoundStatement(context)
{
    public override BoundNodeKind Kind => BoundNodeKind.ReturnStatement;
    public BoundExpression? Value { get; } = value;
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
