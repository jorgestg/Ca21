using System.Collections.Immutable;
using Antlr4.Runtime;
using Ca21.Symbols;

namespace Ca21.Binding;

internal abstract class BoundStatement(ParserRuleContext context)
{
    public ParserRuleContext Context { get; } = context;
}

internal sealed class ControlBlockIdentifier(string name)
{
    public string Name { get; } = name;
}

internal sealed class BoundNopStatement(ParserRuleContext context) : BoundStatement(context);

internal sealed class BoundControlBlockStartStatement(
    ParserRuleContext context,
    ControlBlockIdentifier controlBlockIdentifier,
    bool isLoop = false
) : BoundStatement(context)
{
    public ControlBlockIdentifier ControlBlockIdentifier { get; } = controlBlockIdentifier;
    public bool IsLoop { get; } = isLoop;
}

internal sealed class BoundControlBlockEndStatement(
    ParserRuleContext context,
    ControlBlockIdentifier controlBlockIdentifier
) : BoundStatement(context)
{
    public ControlBlockIdentifier ControlBlockIdentifier { get; } = controlBlockIdentifier;
}

internal sealed class BoundGotoStatement(ParserRuleContext context, ControlBlockIdentifier target)
    : BoundStatement(context)
{
    public ControlBlockIdentifier Target { get; } = target;
}

internal sealed class BoundConditionalGotoStatement(
    ParserRuleContext context,
    BoundExpression condition,
    ControlBlockIdentifier taget,
    bool branchIfFalse = false
) : BoundStatement(context)
{
    public BoundExpression Condition { get; } = condition;
    public ControlBlockIdentifier Target { get; } = taget;
    public bool BranchIfFalse { get; } = branchIfFalse;
}

internal sealed class BoundLocalDeclaration(ParserRuleContext context, LocalSymbol local, BoundExpression? initializer)
    : BoundStatement(context)
{
    public LocalSymbol Local { get; } = local;
    public BoundExpression? Initializer { get; } = initializer;
}

internal sealed class BoundWhileStatement(
    ParserRuleContext context,
    BoundExpression condition,
    BoundBlock body,
    ControlBlockIdentifier continueIdentifier,
    ControlBlockIdentifier breakIdentifier
) : BoundStatement(context)
{
    public BoundExpression Condition { get; } = condition;
    public BoundBlock Body { get; } = body;
    public ControlBlockIdentifier ContinueIdentifier { get; } = continueIdentifier;
    public ControlBlockIdentifier BreakIdentifier { get; } = breakIdentifier;
}

internal sealed class BoundReturnStatement(ParserRuleContext context, BoundExpression? value) : BoundStatement(context)
{
    public BoundExpression? Value { get; } = value;
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
