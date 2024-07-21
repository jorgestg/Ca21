using Ca21.Symbols;

namespace Ca21.Binding;

internal readonly struct BoundBinaryOperator
{
    private static readonly BoundBinaryOperator[] Operators =
    [
        new(BoundBinaryOperatorKind.Multiplication, TypeSymbol.Int32, TypeSymbol.Int32),
        new(BoundBinaryOperatorKind.Division, TypeSymbol.Int32, TypeSymbol.Int32),
        new(BoundBinaryOperatorKind.Remainder, TypeSymbol.Int32, TypeSymbol.Int32),
        new(BoundBinaryOperatorKind.Addition, TypeSymbol.Int32, TypeSymbol.Int32),
        new(BoundBinaryOperatorKind.Subtraction, TypeSymbol.Int32, TypeSymbol.Int32),
        new(BoundBinaryOperatorKind.GreaterThan, TypeSymbol.Int32, TypeSymbol.Bool),
        new(BoundBinaryOperatorKind.GreaterThanOrEqual, TypeSymbol.Int32, TypeSymbol.Bool),
        new(BoundBinaryOperatorKind.LessThan, TypeSymbol.Int32, TypeSymbol.Bool),
        new(BoundBinaryOperatorKind.LessThanOrEqual, TypeSymbol.Int32, TypeSymbol.Bool)
    ];

    private BoundBinaryOperator(BoundBinaryOperatorKind kind, TypeSymbol operandType, TypeSymbol resultType)
    {
        Kind = kind;
        OperandType = operandType;
        ResultType = resultType;
    }

    public BoundBinaryOperatorKind Kind { get; }
    public TypeSymbol OperandType { get; }
    public TypeSymbol ResultType { get; }

    public static bool TryBind(
        BoundBinaryOperatorKind kind,
        TypeSymbol operandType,
        out BoundBinaryOperator boundBinaryOperator
    )
    {
        foreach (var op in Operators)
        {
            if (op.Kind == kind && op.OperandType == operandType)
            {
                boundBinaryOperator = op;
                return true;
            }
        }

        boundBinaryOperator = new BoundBinaryOperator(kind, operandType, TypeSymbol.BadType);
        return false;
    }
}

internal enum BoundBinaryOperatorKind
{
    Multiplication,
    Division,
    Remainder,
    Addition,
    Subtraction,
    GreaterThan,
    GreaterThanOrEqual,
    LessThan,
    LessThanOrEqual
}
