using System.Diagnostics;
using Ca21.Symbols;

namespace Ca21.Binding;

internal static class ConstantFolding
{
    public static object? Fold(BoundBinaryExpression binaryExpression)
    {
        if (binaryExpression.Type == TypeSymbol.Missing)
            return null;

        var leftConstant = binaryExpression.Left.ConstantValue;
        var rightConstant = binaryExpression.Right.ConstantValue;
        if (leftConstant == null || rightConstant == null)
            return null;

        return binaryExpression.Operator.Kind switch
        {
            BoundOperatorKind.Addition => Convert.ToInt64(leftConstant) + Convert.ToInt64(rightConstant),
            BoundOperatorKind.Subtraction => Convert.ToInt64(leftConstant) - Convert.ToInt64(rightConstant),
            BoundOperatorKind.Multiplication => Convert.ToInt64(leftConstant) * Convert.ToInt64(rightConstant),
            BoundOperatorKind.Division => Convert.ToInt64(leftConstant) / Convert.ToInt64(rightConstant),
            BoundOperatorKind.Remainder => Convert.ToInt64(leftConstant) % Convert.ToInt64(rightConstant),
            BoundOperatorKind.Less => Convert.ToInt64(leftConstant) < Convert.ToInt64(rightConstant),
            BoundOperatorKind.LessOrEqual => Convert.ToInt64(leftConstant) <= Convert.ToInt64(rightConstant),
            BoundOperatorKind.Greater => Convert.ToInt64(leftConstant) > Convert.ToInt64(rightConstant),
            BoundOperatorKind.GreaterOrEqual => Convert.ToInt64(leftConstant) >= Convert.ToInt64(rightConstant),
            BoundOperatorKind.LogicalAnd => (bool)leftConstant && (bool)rightConstant,
            BoundOperatorKind.LogicalOr => (bool)leftConstant || (bool)rightConstant,
            BoundOperatorKind.Equality => Equals(leftConstant, rightConstant),
            BoundOperatorKind.Inequality => !Equals(leftConstant, rightConstant),
            _ => throw new UnreachableException(),
        };
    }

    public static object? Fold(BoundUnaryExpression unaryExpression)
    {
        if (unaryExpression.Type == TypeSymbol.Missing)
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
