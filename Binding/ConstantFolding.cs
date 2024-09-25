using System.Diagnostics;

namespace Ca21.Binding;

internal static class ConstantFolding
{
    public static BoundConstant Fold(BoundBinaryExpression binaryExpression)
    {
        if (binaryExpression.Type == Symbols.TypeSymbol.Missing)
            return default;

        var leftConstant = binaryExpression.Left.ConstantValue;
        var rightConstant = binaryExpression.Right.ConstantValue;
        if (!leftConstant.HasValue || !rightConstant.HasValue)
            return default;

        var l = leftConstant.Value;
        var r = rightConstant.Value;
        return binaryExpression.Operator.Kind switch
        {
            BoundOperatorKind.Addition => new BoundConstant((int)l + (int)r),
            BoundOperatorKind.Subtraction => new BoundConstant((int)l - (int)r),
            BoundOperatorKind.Multiplication => new BoundConstant((int)l * (int)r),
            BoundOperatorKind.Division => new BoundConstant((int)l / (int)r),
            BoundOperatorKind.Remainder => new BoundConstant((int)l % (int)r),
            BoundOperatorKind.Less => new BoundConstant((int)l < (int)r),
            BoundOperatorKind.LessOrEqual => new BoundConstant((int)l <= (int)r),
            BoundOperatorKind.Greater => new BoundConstant((int)l > (int)r),
            BoundOperatorKind.GreaterOrEqual => new BoundConstant((int)l >= (int)r),
            _ => throw new UnreachableException(),
        };
    }

    public static BoundConstant Fold(BoundUnaryExpression unaryExpression)
    {
        if (unaryExpression.Type == Symbols.TypeSymbol.Missing)
            return default;

        if (!unaryExpression.Operand.ConstantValue.HasValue)
            return default;

        var value = unaryExpression.Operand.ConstantValue.Value;
        return unaryExpression.Operator.Kind switch
        {
            BoundOperatorKind.LogicalNot => new BoundConstant(!(bool)value),
            BoundOperatorKind.Negation => new BoundConstant(-(int)value),
            _ => throw new UnreachableException(),
        };
    }
}
