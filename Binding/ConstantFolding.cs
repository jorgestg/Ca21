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
            BoundBinaryOperatorKind.Addition => new BoundConstant((int)l + (int)r),
            BoundBinaryOperatorKind.Subtraction => new BoundConstant((int)l - (int)r),
            BoundBinaryOperatorKind.Multiplication => new BoundConstant((int)l * (int)r),
            BoundBinaryOperatorKind.Division => new BoundConstant((int)l / (int)r),
            BoundBinaryOperatorKind.Remainder => new BoundConstant((int)l % (int)r),
            BoundBinaryOperatorKind.Less => new BoundConstant((int)l < (int)r),
            BoundBinaryOperatorKind.LessOrEqual => new BoundConstant((int)l <= (int)r),
            BoundBinaryOperatorKind.Greater => new BoundConstant((int)l > (int)r),
            BoundBinaryOperatorKind.GreaterOrEqual => new BoundConstant((int)l >= (int)r),
            _ => throw new UnreachableException(),
        };
    }
}
