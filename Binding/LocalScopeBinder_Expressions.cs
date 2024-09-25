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

    private void PushEnvironmentType(TypeSymbol type)
    {
        _environmentType ??= new Stack<TypeSymbol>();
        _environmentType.Push(type);
    }

    private TypeSymbol? PeekEnvironmentType() => _environmentType?.TryPeek(out var type) == true ? type : null;

    private void PopEnvironmentType() => _environmentType?.Pop();

    public BoundExpression BindBlockExpression(
        BlockExpressionContext context,
        TypeSymbol environmentType,
        DiagnosticList diagnostics
    )
    {
        PushEnvironmentType(environmentType);
        var expression = BindBlockExpression(context, diagnostics);
        PopEnvironmentType();
        return expression;
    }

    private BoundBlockExpression BindBlockExpression(BlockExpressionContext context, DiagnosticList diagnostics)
    {
        var localScopeBinder = new LocalScopeBinder(this);
        var statements = context
            ._Statements.Select(statement => localScopeBinder.BindStatement(statement, diagnostics))
            .ToImmutableArray();

        var tail = context.Tail == null ? null : localScopeBinder.BindExpression(context.Tail, diagnostics);
        return new BoundBlockExpression(context, statements, tail);
    }

    public BoundExpression BindExpression(
        ExpressionContext context,
        TypeSymbol expectedType,
        DiagnosticList diagnostics
    )
    {
        PushEnvironmentType(expectedType);
        var expression = BindExpression(context, diagnostics);
        PopEnvironmentType();
        return expression;
    }

    public BoundExpression BindExpression(ExpressionContext context, DiagnosticList diagnostics)
    {
        return context switch
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
            AssignmentExpressionContext c => BindAssignmentExpression(c, diagnostics),
            _ => throw new UnreachableException()
        };
    }

    private BoundLiteralExpression BindLiteral(LiteralContext context, DiagnosticList diagnostics)
    {
        (object value, TypeSymbol type) = context switch
        {
            IntegerLiteralContext c => BindIntegerLiteral(c),
            TrueLiteralContext => (true, TypeSymbol.Bool),
            FalseLiteralContext => (false, TypeSymbol.Bool),
            StringLiteralContext c => (c.Value.Text.Trim('"'), TypeSymbol.String),
            _ => throw new UnreachableException()
        };

        return new BoundLiteralExpression(context, value, type);
    }

    private (object, TypeSymbol) BindIntegerLiteral(IntegerLiteralContext context)
    {
        var environmentType = PeekEnvironmentType() ?? TypeSymbol.Int32;
        switch (environmentType.NativeType)
        {
            case NativeType.Int64:
                return (long.Parse(context.Value.Text), TypeSymbol.Int64);

            default:
                return (int.Parse(context.Value.Text), TypeSymbol.Int32);
        }
    }

    private BoundNameExpression BindNameExpression(NameExpressionContext context, DiagnosticList diagnostics)
    {
        var referencedSymbol = Lookup(context.Name.Text);
        if (referencedSymbol == null)
        {
            diagnostics.Add(context, DiagnosticMessages.NameNotFound(context.Name.Text));
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
        if (nameExpression.ReferencedSymbol.Kind != SymbolKind.Function)
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

        if (nameExpression.ReferencedSymbol == FunctionSymbol.Missing)
        {
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

            var argument = BindExpression(arguments[i], parameter.Type, diagnostics);
            TypeCheck(argument.Context, parameter.Type, argument.Type, diagnostics);
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
        if (left.Type == TypeSymbol.Missing)
            return new BoundAccessExpression(context, left, FieldSymbol.Missing);

        if (
            left.Type is not StructureSymbol structureSymbol
            || !structureSymbol.FieldMap.TryGetValue(context.Right.Text, out var referencedField)
        )
        {
            diagnostics.Add(context.Right, DiagnosticMessages.TypeDoesNotContainMember(left.Type, context.Right.Text));

            referencedField = FieldSymbol.Missing;
        }

        return new BoundAccessExpression(context, left, referencedField);
    }

    private BoundStructureLiteralExpression BindStructureLiteralExpression(
        StructureLiteralExpressionContext context,
        DiagnosticList diagnostics
    )
    {
        var referencedType = BindType(context.Structure, diagnostics);
        var structure = referencedType as StructureSymbol;
        if (referencedType != TypeSymbol.Missing && structure == null)
        {
            diagnostics.Add(context.Structure, DiagnosticMessages.NameIsNotAType(context.Structure.GetText()));
        }

        var fieldInitializers = new ArrayBuilder<BoundFieldInitializer>(context._Fields.Count);
        foreach (var fieldInitializerContext in context._Fields)
        {
            IToken name;
            BoundExpression value;
            switch (fieldInitializerContext)
            {
                case AssignmentFieldInitializerContext c:
                    name = c.Name;
                    value = BindExpressionOrBlock(c.Value, diagnostics);
                    break;

                case NameOnlyFieldInitializerContext c:
                    name = c.Name;
                    var referencedSymbol = Lookup(name.Text) ?? Symbol.Missing;
                    if (referencedSymbol == Symbol.Missing)
                        diagnostics.Add(name, DiagnosticMessages.NameNotFound(c.Name.Text));

                    value = new BoundNameExpression(c, referencedSymbol);
                    break;

                default:
                    throw new UnreachableException();
            }

            if (structure == null)
                continue;

            var field = structure.FieldMap.GetValueOrDefault(name.Text);
            if (field == null)
            {
                field = FieldSymbol.Missing;
                diagnostics.Add(name, DiagnosticMessages.TypeDoesNotContainMember(structure, name.Text));
            }

            TypeCheck(fieldInitializerContext, field.Type, value.Type, diagnostics);

            var fieldInitializer = new BoundFieldInitializer(context, field, value);
            fieldInitializers.Add(fieldInitializer);
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
            _ => throw new UnreachableException()
        };

        if (!BoundOperator.TryBind(binaryOpKind, boundLeft.Type, out var boundOp))
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
            return new BoundAssignmentExpression(context, Symbol.Missing, BindExpression(context.Value, diagnostics));
        }

        if (name.ReferencedSymbol.Kind is not SymbolKind.Local and not SymbolKind.Field)
        {
            diagnostics.Add(assignee.Context, DiagnosticMessages.SymbolIsNotAssignable(name.ReferencedSymbol.Name));
            return new BoundAssignmentExpression(context, Symbol.Missing, BindExpression(context.Value, diagnostics));
        }

        var value = BindExpression(context.Value, name.ReferencedSymbol.Type, diagnostics);
        if (name.ReferencedSymbol.Kind == SymbolKind.Local && !((LocalSymbol)name.ReferencedSymbol).IsMutable)
            diagnostics.Add(assignee.Context, DiagnosticMessages.NameIsImmutable(name.ReferencedSymbol.Name));

        TypeCheck(context, name.ReferencedSymbol.Type, value.Type, diagnostics);
        return new BoundAssignmentExpression(context, name.ReferencedSymbol, value);
    }
}
