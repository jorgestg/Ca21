using System.Diagnostics;

namespace Ca21.Binding;

internal static class ConstantFolding
{
    public static object? Fold(BoundBinaryExpression binaryExpression)
    {
        if (binaryExpression.Type == Symbols.TypeSymbol.Missing)
            return null;

        var leftConstant = binaryExpression.Left.ConstantValue;
        var rightConstant = binaryExpression.Right.ConstantValue;
        if (leftConstant == null || rightConstant == null)
            return null;

        var l = Convert.ToInt64(leftConstant);
        var r = Convert.ToInt64(rightConstant);
        return binaryExpression.Operator.Kind switch
        {
            BoundOperatorKind.Addition => l + r,
            BoundOperatorKind.Subtraction => l - r,
            BoundOperatorKind.Multiplication => l * r,
            BoundOperatorKind.Division => l / r,
            BoundOperatorKind.Remainder => l % r,
            BoundOperatorKind.Less => l < r,
            BoundOperatorKind.LessOrEqual => l <= r,
            BoundOperatorKind.Greater => l > r,
            BoundOperatorKind.GreaterOrEqual => l >= r,
            _ => throw new UnreachableException(),
        };
    }

    public static object? Fold(BoundUnaryExpression unaryExpression)
    {
        if (unaryExpression.Type == Symbols.TypeSymbol.Missing)
            return null;

        if (unaryExpression.Operand.ConstantValue == null)
            return null;

        return unaryExpression.Operator.Kind switch
        {
            BoundOperatorKind.LogicalNot => !(bool)unaryExpression.Operand.ConstantValue,
            BoundOperatorKind.Negation => -Convert.ToInt64(unaryExpression.Operand.ConstantValue),
            _ => throw new UnreachableException(),
        };
    }
}
