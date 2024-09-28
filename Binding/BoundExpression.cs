using System.Collections.Immutable;
using Antlr4.Runtime;
using Ca21.Symbols;

namespace Ca21.Binding;

internal abstract class BoundExpression(ParserRuleContext context) : BoundNode
{
    public ParserRuleContext Context { get; } = context;
    public abstract TypeSymbol Type { get; }
    public virtual object? ConstantValue => default;
}

internal sealed class BoundBlockExpression(
    ParserRuleContext context,
    ImmutableArray<BoundStatement> statements,
    BoundExpression? tailExpression
) : BoundExpression(context)
{
    public override BoundNodeKind Kind => BoundNodeKind.BlockExpression;
    public override TypeSymbol Type => TailExpression?.Type ?? TypeSymbol.Unit;
    public ImmutableArray<BoundStatement> Statements { get; } = statements;
    public BoundExpression? TailExpression { get; } = tailExpression;
}

internal sealed class BoundCastExpression(ParserRuleContext context, BoundExpression expression, TypeSymbol type)
    : BoundExpression(context)
{
    public override BoundNodeKind Kind => BoundNodeKind.CastExpression;
    public BoundExpression Expression { get; } = expression;
    public override TypeSymbol Type { get; } = type;
}

internal sealed class BoundLiteralExpression(ParserRuleContext context, object? value, TypeSymbol type)
    : BoundExpression(context)
{
    public override BoundNodeKind Kind => BoundNodeKind.LiteralExpression;
    public override TypeSymbol Type { get; } = type;
    public override object? ConstantValue => Value;
    public object? Value { get; } = value;
}

internal sealed class BoundCallExpression(
    ParserRuleContext context,
    FunctionSymbol function,
    ImmutableArray<BoundExpression> arguments
) : BoundExpression(context)
{
    public override BoundNodeKind Kind => BoundNodeKind.CallExpression;
    public override TypeSymbol Type => Function.ReturnType;
    public FunctionSymbol Function { get; } = function;
    public ImmutableArray<BoundExpression> Arguments { get; } = arguments;
}

internal sealed class BoundAccessExpression(
    ParserRuleContext context,
    BoundExpression left,
    FieldSymbol referencedField
) : BoundExpression(context)
{
    public override BoundNodeKind Kind => BoundNodeKind.AccessExpression;
    public override TypeSymbol Type => ReferencedField.Type;
    public BoundExpression Left { get; } = left;
    public FieldSymbol ReferencedField { get; } = referencedField;
}

internal sealed class BoundStructureLiteralExpression(
    ParserRuleContext context,
    TypeSymbol structure,
    ImmutableArray<BoundFieldInitializer> fieldInitializers
) : BoundExpression(context)
{
    public override BoundNodeKind Kind => BoundNodeKind.StructureLiteralExpression;
    public override TypeSymbol Type => Structure;
    public TypeSymbol Structure { get; } = structure;
    public ImmutableArray<BoundFieldInitializer> FieldInitializers { get; } = fieldInitializers;
}

internal readonly struct BoundFieldInitializer(
    ParserRuleContext context,
    FieldSymbol field,
    BoundExpression initializer
)
{
    public ParserRuleContext Context { get; } = context;
    public FieldSymbol Field { get; } = field;
    public BoundExpression Value { get; } = initializer;
}

internal sealed class BoundUnaryExpression : BoundExpression
{
    public BoundUnaryExpression(ParserRuleContext context, BoundOperator @operator, BoundExpression operand)
        : base(context)
    {
        Operator = @operator;
        Operand = operand;
        ConstantValue = ConstantFolding.Fold(this);
    }

    public override BoundNodeKind Kind => BoundNodeKind.UnaryExpression;
    public override TypeSymbol Type => Operator.ResultType;
    public override object? ConstantValue { get; }
    public BoundOperator Operator { get; }
    public BoundExpression Operand { get; }
}

internal sealed class BoundNameExpression(ParserRuleContext context, Symbol referencedSymbol) : BoundExpression(context)
{
    public override BoundNodeKind Kind => BoundNodeKind.NameExpression;
    public override TypeSymbol Type => ReferencedSymbol.Type;
    public Symbol ReferencedSymbol { get; } = referencedSymbol;
}

internal sealed class BoundBinaryExpression : BoundExpression
{
    public BoundBinaryExpression(
        ParserRuleContext context,
        BoundExpression left,
        BoundOperator @operator,
        BoundExpression right
    )
        : base(context)
    {
        Left = left;
        Operator = @operator;
        Right = right;
        ConstantValue = ConstantFolding.Fold(this);
    }

    public override BoundNodeKind Kind => BoundNodeKind.BinaryExpression;
    public override TypeSymbol Type => Operator.ResultType;
    public override object? ConstantValue { get; }
    public BoundExpression Left { get; }
    public BoundOperator Operator { get; }
    public BoundExpression Right { get; }
}

internal sealed class BoundAssignmentExpression(ParserRuleContext context, Symbol assignee, BoundExpression value)
    : BoundExpression(context)
{
    public override BoundNodeKind Kind => BoundNodeKind.AssignmentExpression;
    public override TypeSymbol Type => TypeSymbol.Unit;
    public Symbol Assignee { get; } = assignee;
    public BoundExpression Value { get; } = value;
}
