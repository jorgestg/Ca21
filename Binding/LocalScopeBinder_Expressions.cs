using System.Collections.Immutable;
using System.Diagnostics;
using Antlr4.Runtime;
using Ca21.Diagnostics;
using Ca21.Symbols;
using static Ca21.Antlr.Ca21Parser;

namespace Ca21.Binding;

internal sealed partial class LocalScopeBinder
{
    private Stack<TypeSymbol>? _environmentType;
    private Stack<TypeSymbol> EnviromentTypeStack => _environmentType ??= new();

    private TypeSymbol? PeekEnvironmentType() => _environmentType?.TryPeek(out var type) == true ? type : null;

    private BoundExpression BindExpressionOrBlock(
        ExpressionOrBlockContext context,
        DiagnosticList diagnostics,
        TypeSymbol? environmentType = null
    )
    {
        return context switch
        {
            BlockExpressionContext c => BindBlockExpression(c, diagnostics, environmentType),
            NonBlockExpressionContext c => BindExpression(c.Expression, diagnostics, environmentType),
            _ => throw new UnreachableException()
        };
    }

    private BoundBlockExpression BindBlockExpression(
        BlockExpressionContext context,
        DiagnosticList diagnostics,
        TypeSymbol? environmentType = null
    )
    {
        var localScopeBinder = new LocalScopeBinder(this);
        var statements = context
            ._Statements.Select(statement => localScopeBinder.BindStatement(statement, diagnostics))
            .ToImmutableArray();

        var tail =
            context.Tail == null ? null : localScopeBinder.BindExpression(context.Tail, diagnostics, environmentType);
        return new BoundBlockExpression(context, statements, tail);
    }

    private BoundExpression BindExpression(
        ExpressionContext context,
        DiagnosticList diagnostics,
        TypeSymbol? environmentType = null
    )
    {
        if (environmentType != null)
            EnviromentTypeStack.Push(environmentType);

        BoundExpression expression = context switch
        {
            LiteralExpressionContext c => BindLiteral(c.Literal, diagnostics),
            NameExpressionContext c => BindNameExpression(c, diagnostics),
            CallExpressionContext c => BindCallExpression(c, diagnostics),
            AccessExpressionContext c => BindAccessExpression(c, diagnostics),
            StructureLiteralExpressionContext c => BindStructureLiteralExpression(c, diagnostics),
            UnaryExpressionContext c => BindUnaryExpression(c, diagnostics),
            FactorExpressionContext c => BindBinaryExpression(c, c.Left, c.Operator, c.Right, diagnostics),
            TermExpressionContext c => BindBinaryExpression(c, c.Left, c.Operator, c.Right, diagnostics),
            ComparisonExpressionContext c => BindBinaryExpression(c, c.Left, c.Operator, c.Right, diagnostics),
            EqualityExpressionContext c => BindBinaryExpression(c, c.Left, c.Operator, c.Right, diagnostics),
            LogicalAndExpressionContext c => BindBinaryExpression(c, c.Left, c.Operator, c.Right, diagnostics),
            LogicalOrExpressionContext c => BindBinaryExpression(c, c.Left, c.Operator, c.Right, diagnostics),
            AssignmentExpressionContext c => BindAssignmentExpression(c, diagnostics),
            _ => throw new UnreachableException()
        };

        if (environmentType != null)
        {
            EnviromentTypeStack.Pop();
            expression = BindConversion(expression, environmentType, diagnostics);
        }

        return expression;
    }

    private BoundLiteralExpression BindLiteral(LiteralContext context, DiagnosticList diagnostics)
    {
        (object? value, TypeSymbol type) = context switch
        {
            IntegerLiteralContext c => BindIntegerLiteral(c, diagnostics),
            TrueLiteralContext => (true, TypeSymbol.Bool),
            FalseLiteralContext => (false, TypeSymbol.Bool),
            StringLiteralContext c => (c.Value.Text.Trim('"'), TypeSymbol.String),
            _ => throw new UnreachableException()
        };

        return new BoundLiteralExpression(context, value, type);
    }

    private (object?, TypeSymbol) BindIntegerLiteral(IntegerLiteralContext context, DiagnosticList diagnostics)
    {
        var environmentType = PeekEnvironmentType() ?? TypeSymbol.Int32;
        var literal = context.Value.Text.Replace("_", "");
        switch (environmentType.TypeKind)
        {
            case TypeKind.USize:
                if (nuint.TryParse(literal, out var nuintValue))
                    return (nuintValue, TypeSymbol.USize);

                break;

            case TypeKind.Int64:
                if (long.TryParse(literal, out var longValue))
                    return (longValue, TypeSymbol.Int64);

                break;

            case TypeKind.Int32:
                if (int.TryParse(literal, out var intValue))
                    return (intValue, TypeSymbol.Int32);

                break;

            default:
                environmentType = TypeSymbol.Int32;
                goto case TypeKind.Int32;
        }

        diagnostics.Add(context.Value, DiagnosticMessages.ValueDoesNotFitInType(environmentType));
        return (null, environmentType);
    }

    private BoundNameExpression BindNameExpression(NameExpressionContext context, DiagnosticList diagnostics)
    {
        if (TryBindType(context.Name, out var typeSymbol))
            return new BoundNameExpression(context, typeSymbol);

        if (context.Name is not SimpleNameTypeReferenceContext c)
            return new BoundNameExpression(context, TypeSymbol.Missing);

        var referencedSymbol = Lookup(c.Name.Text);
        if (referencedSymbol == null)
        {
            diagnostics.Add(context, DiagnosticMessages.NameNotFound(c.Name.Text));
            referencedSymbol = Symbol.Missing;
        }

        return new BoundNameExpression(context, referencedSymbol);
    }

    private BoundCallExpression BindCallExpression(CallExpressionContext context, DiagnosticList diagnostics)
    {
        var callee = BindExpression(context.Callee, diagnostics);
        if (callee.Kind != BoundNodeKind.NameExpression)
        {
            diagnostics.Add(context, DiagnosticMessages.ExpressionIsNotCallable);
            return new BoundCallExpression(
                context.Callee,
                FunctionSymbol.Missing,
                BindArgumentsUnchecked(context, diagnostics)
            );
        }

        var nameExpression = (BoundNameExpression)callee;
        if (nameExpression.ReferencedSymbol.SymbolKind != SymbolKind.Function)
        {
            if (nameExpression.ReferencedSymbol != Symbol.Missing)
            {
                diagnostics.Add(
                    nameExpression.Context,
                    DiagnosticMessages.ValueOfTypeIsNotCallable(nameExpression.ReferencedSymbol.Type)
                );
            }

            return new BoundCallExpression(
                context,
                FunctionSymbol.Missing,
                BindArgumentsUnchecked(context, diagnostics)
            );
        }

        var functionSymbol = (FunctionSymbol)nameExpression.ReferencedSymbol;
        if (context.ArgumentList == null)
            return new BoundCallExpression(context, functionSymbol, []);

        var arguments = context.ArgumentList._Arguments;
        var argumentsBuilder = new ArrayBuilder<BoundExpression>(arguments.Count);
        for (var i = 0; i < arguments.Count; i++)
        {
            var parameter = functionSymbol.Parameters.ElementAtOrDefault(i);
            if (parameter == null)
            {
                diagnostics.Add(context, DiagnosticMessages.FunctionOnlyExpectsNArguments(functionSymbol));
                argumentsBuilder.Add(BindExpression(arguments[i], diagnostics));
                continue;
            }

            var argument = BindExpression(arguments[i], diagnostics, parameter.Type);
            argumentsBuilder.Add(argument);
        }

        return new BoundCallExpression(context, functionSymbol, argumentsBuilder.MoveToImmutable());

        ImmutableArray<BoundExpression> BindArgumentsUnchecked(
            CallExpressionContext context,
            DiagnosticList diagnostics
        )
        {
            if (context.ArgumentList == null)
                return [];

            var argumentsBuilder = new ArrayBuilder<BoundExpression>(context.ArgumentList._Arguments.Count);
            foreach (var argument in context.ArgumentList._Arguments)
            {
                var boundArgument = BindExpression(argument, diagnostics);
                argumentsBuilder.Add(boundArgument);
            }

            return argumentsBuilder.MoveToImmutable();
        }
    }

    private BoundAccessExpression BindAccessExpression(AccessExpressionContext context, DiagnosticList diagnostics)
    {
        var left = BindExpression(context.Left, diagnostics);
        var type = left is BoundNameExpression { ReferencedSymbol.SymbolKind: SymbolKind.Type } nameExpression
            ? (TypeSymbol)nameExpression.ReferencedSymbol
            : left.Type;

        if (type == TypeSymbol.Missing)
            return new BoundAccessExpression(context, left, TypeMemberSymbol.Missing);

        if (type.TryGetMember(context.Right.Text, out var member))
            return new BoundAccessExpression(context, left, member);

        diagnostics.Add(context.Right, DiagnosticMessages.TypeDoesNotContainMember(left.Type, context.Right.Text));
        return new BoundAccessExpression(context, left, TypeMemberSymbol.Missing);
    }

    private BoundStructureLiteralExpression BindStructureLiteralExpression(
        StructureLiteralExpressionContext context,
        DiagnosticList diagnostics
    )
    {
        var referencedType = BindType(context.Structure, diagnostics);
        var fieldInitializers = new ArrayBuilder<BoundFieldInitializer>(context._Fields.Count);
        foreach (var fieldInitializerContext in context._Fields)
        {
            TypeMemberSymbol field;
            IToken name;
            BoundExpression value;

            switch (fieldInitializerContext)
            {
                case AssignmentFieldInitializerContext c:
                    name = c.Name;
                    referencedType.TryGetMember(name.Text, out field);
                    value = BindExpressionOrBlock(c.Value, diagnostics, field.Type);
                    break;

                case NameOnlyFieldInitializerContext c:
                    name = c.Name;

                    var referencedSymbol = Lookup(name.Text) ?? Symbol.Missing;
                    if (referencedSymbol == Symbol.Missing)
                        diagnostics.Add(c.Name, DiagnosticMessages.NameNotFound(name.Text));

                    referencedType.TryGetMember(name.Text, out field);
                    value = BindConversion(new BoundNameExpression(c, referencedSymbol), field.Type, diagnostics);
                    break;

                default:
                    throw new UnreachableException();
            }

            if (field == TypeMemberSymbol.Missing && referencedType != TypeSymbol.Missing)
                diagnostics.Add(name, DiagnosticMessages.TypeDoesNotContainMember(referencedType, name.Text));

            fieldInitializers.Add(new BoundFieldInitializer(context, (FieldSymbol)field, value));
        }

        return new BoundStructureLiteralExpression(context, referencedType, fieldInitializers.MoveToImmutable());
    }

    private BoundUnaryExpression BindUnaryExpression(UnaryExpressionContext context, DiagnosticList diagnostics)
    {
        var operand = BindExpression(context.Operand, diagnostics);
        var operatorKind = context.Operator.Type switch
        {
            Minus => BoundOperatorKind.Negation,
            Bang => BoundOperatorKind.LogicalNot,
            _ => throw new UnreachableException()
        };

        if (!BoundOperator.TryBind(operatorKind, operand.Type, out var boundOp))
        {
            diagnostics.Add(
                context.Operator,
                DiagnosticMessages.UnaryOperatorTypeMismatch(context.Operator.Text, operand.Type)
            );
        }

        return new BoundUnaryExpression(context, boundOp, operand);
    }

    private BoundBinaryExpression BindBinaryExpression(
        ExpressionContext context,
        ExpressionContext left,
        IToken op,
        ExpressionContext right,
        DiagnosticList diagnostics
    )
    {
        var boundLeft = BindExpression(left, diagnostics);
        var boundRight = BindExpression(right, diagnostics);
        var binaryOpKind = op.Type switch
        {
            Star => BoundOperatorKind.Multiplication,
            Slash => BoundOperatorKind.Division,
            Percentage => BoundOperatorKind.Remainder,
            Plus => BoundOperatorKind.Addition,
            Minus => BoundOperatorKind.Subtraction,
            GreaterThan => BoundOperatorKind.Greater,
            GreaterThanOrEqual => BoundOperatorKind.GreaterOrEqual,
            LessThan => BoundOperatorKind.Less,
            LessThanOrEqual => BoundOperatorKind.LessOrEqual,
            DoubleEqual => BoundOperatorKind.Equality,
            BangEqual => BoundOperatorKind.Inequality,
            DoubleAmpersand => BoundOperatorKind.LogicalAnd,
            DoublePipe => BoundOperatorKind.LogicalOr,
            _ => throw new UnreachableException()
        };

        if (!BoundOperator.TryBind(binaryOpKind, boundLeft.Type, boundRight.Type, out var boundOp))
        {
            diagnostics.Add(
                context,
                DiagnosticMessages.BinaryOperatorTypeMismatch(op.Text, boundLeft.Type, boundRight.Type)
            );
        }

        return new BoundBinaryExpression(context, boundLeft, boundOp, boundRight);
    }

    private BoundAssignmentExpression BindAssignmentExpression(
        AssignmentExpressionContext context,
        DiagnosticList diagnostics
    )
    {
        var assignee = BindExpression(context.Assignee, diagnostics);
        if (assignee is not BoundNameExpression name)
        {
            diagnostics.Add(assignee.Context, DiagnosticMessages.ExpressionIsNotAssignable);
            return new BoundAssignmentExpression(
                context,
                Symbol.Missing,
                BindExpression(context.Expression, diagnostics)
            );
        }

        if (name.ReferencedSymbol.SymbolKind is not SymbolKind.Local and not SymbolKind.Field)
        {
            diagnostics.Add(assignee.Context, DiagnosticMessages.SymbolIsNotAssignable(name.ReferencedSymbol.Name));
            return new BoundAssignmentExpression(
                context,
                Symbol.Missing,
                BindExpression(context.Expression, diagnostics)
            );
        }

        var value = BindExpression(context.Expression, diagnostics, name.ReferencedSymbol.Type);
        if (name.ReferencedSymbol.SymbolKind == SymbolKind.Local && !((LocalSymbol)name.ReferencedSymbol).IsMutable)
            diagnostics.Add(assignee.Context, DiagnosticMessages.NameIsImmutable(name.ReferencedSymbol.Name));

        return new BoundAssignmentExpression(context, name.ReferencedSymbol, value);
    }
}
