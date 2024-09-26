using Ca21.Symbols;

namespace Ca21.Binding;

internal enum BoundOperatorKind
{
    LogicalNot,
    Negation,
    Multiplication,
    Division,
    Remainder,
    Addition,
    Subtraction,
    Greater,
    GreaterOrEqual,
    Less,
    LessOrEqual
}

internal readonly struct BoundOperator
{
    private static readonly BoundOperator[] Operators =
    [
        new(BoundOperatorKind.LogicalNot, TypeSymbol.Bool, TypeSymbol.Bool),
        // Int32
        new(BoundOperatorKind.Negation, TypeSymbol.Int32, TypeSymbol.Int32),
        new(BoundOperatorKind.Multiplication, TypeSymbol.Int32, TypeSymbol.Int32),
        new(BoundOperatorKind.Division, TypeSymbol.Int32, TypeSymbol.Int32),
        new(BoundOperatorKind.Remainder, TypeSymbol.Int32, TypeSymbol.Int32),
        new(BoundOperatorKind.Addition, TypeSymbol.Int32, TypeSymbol.Int32),
        new(BoundOperatorKind.Subtraction, TypeSymbol.Int32, TypeSymbol.Int32),
        new(BoundOperatorKind.Greater, TypeSymbol.Int32, TypeSymbol.Bool),
        new(BoundOperatorKind.GreaterOrEqual, TypeSymbol.Int32, TypeSymbol.Bool),
        new(BoundOperatorKind.Less, TypeSymbol.Int32, TypeSymbol.Bool),
        new(BoundOperatorKind.LessOrEqual, TypeSymbol.Int32, TypeSymbol.Bool),
        // Int64
        new(BoundOperatorKind.Negation, TypeSymbol.Int64, TypeSymbol.Int64),
        new(BoundOperatorKind.Multiplication, TypeSymbol.Int64, TypeSymbol.Int64),
        new(BoundOperatorKind.Division, TypeSymbol.Int64, TypeSymbol.Int64),
        new(BoundOperatorKind.Remainder, TypeSymbol.Int64, TypeSymbol.Int64),
        new(BoundOperatorKind.Addition, TypeSymbol.Int64, TypeSymbol.Int64),
        new(BoundOperatorKind.Subtraction, TypeSymbol.Int64, TypeSymbol.Int64),
        new(BoundOperatorKind.Greater, TypeSymbol.Int64, TypeSymbol.Bool),
        new(BoundOperatorKind.GreaterOrEqual, TypeSymbol.Int64, TypeSymbol.Bool),
        new(BoundOperatorKind.Less, TypeSymbol.Int64, TypeSymbol.Bool),
        new(BoundOperatorKind.LessOrEqual, TypeSymbol.Int64, TypeSymbol.Bool)
    ];

    private BoundOperator(BoundOperatorKind kind, TypeSymbol operandType, TypeSymbol resultType)
    {
        Kind = kind;
        OperandType = operandType;
        ResultType = resultType;
    }

    public BoundOperatorKind Kind { get; }
    public TypeSymbol OperandType { get; }
    public TypeSymbol ResultType { get; }

    public static bool TryBind(
        BoundOperatorKind kind,
        TypeSymbol left,
        TypeSymbol right,
        out BoundOperator boundOperator
    )
    {
        var operandType = TypeSymbol.Unify(left, right);
        if (operandType == null)
        {
            boundOperator = new BoundOperator(kind, TypeSymbol.Missing, TypeSymbol.Missing);
            return false;
        }

        return TryBind(kind, operandType, out boundOperator);
    }

    public static bool TryBind(BoundOperatorKind kind, TypeSymbol operandType, out BoundOperator boundOperator)
    {
        foreach (var op in Operators)
        {
            if (op.Kind == kind && op.OperandType == operandType)
            {
                boundOperator = op;
                return true;
            }
        }

        boundOperator = new BoundOperator(kind, operandType, TypeSymbol.Missing);
        return false;
    }
}
