using System.Collections.Immutable;
using System.Diagnostics;
using Antlr4.Runtime;
using Ca21.Diagnostics;
using Ca21.Symbols;
using static Ca21.Antlr.Ca21Parser;

namespace Ca21.Binding;

internal sealed class LocalScopeBinder(Binder parent) : Binder
{
    private readonly Dictionary<string, Symbol> _locals = new();

    public override Binder Parent { get; } = parent;

    public override Symbol? Lookup(string name)
    {
        return _locals.GetValueOrDefault(name) ?? Parent.Lookup(name);
    }

    public BoundStatement BindStatement(StatementContext context, DiagnosticList diagnostics)
    {
        return context switch
        {
            LocalDeclarationStatementContext c => BindLocalDeclaration(c.Declaration, diagnostics),
            WhileStatementContext c => BindWhileStatement(c, diagnostics),
            ReturnStatementContext c => BindReturnStatement(c, diagnostics),
            BlockStatementContext c => BindBlock(c.Block, diagnostics),
            ExpressionStatementContext c => BindExpressionStatement(c.Expression, diagnostics),
            _ => throw new UnreachableException()
        };
    }

    private BoundLocalDeclaration BindLocalDeclaration(LocalDeclarationContext context, DiagnosticList diagnostics)
    {
        var initializer = BindExpressionOrBlock(context.Value, diagnostics);
        var local = new SourceLocalSymbol(context, initializer.Type);
        _locals[local.Name] = local;
        return new BoundLocalDeclaration(context, local, initializer);
    }

    private BoundWhileStatement BindWhileStatement(WhileStatementContext context, DiagnosticList diagnostics)
    {
        var condition = BindExpression(context.Condition, diagnostics);
        TypeCheck(context, TypeSymbol.Bool, condition.Type, diagnostics);
        var body = BindBlock(context.Body, diagnostics);
        return new BoundWhileStatement(context, condition, body, new LabelSymbol(), new LabelSymbol());
    }

    private BoundReturnStatement BindReturnStatement(ReturnStatementContext context, DiagnosticList diagnostics)
    {
        var returnValue = BindExpressionOrBlock(context.Value, diagnostics);
        TypeCheck(context, GetReturnType(), returnValue.Type, diagnostics);
        return new BoundReturnStatement(context, returnValue);
    }

    private BoundExpressionStatement BindExpressionStatement(ExpressionContext context, DiagnosticList diagnostics)
    {
        var expression = BindExpression(context, diagnostics);
        if (expression is not BoundAssignmentExpression)
            diagnostics.Add(context.SourceInterval, DiagnosticMessages.PureExpressionAsStatement);

        return new BoundExpressionStatement(context, expression);
    }

    private BoundExpression BindExpressionOrBlock(ExpressionOrBlockContext context, DiagnosticList diagnostics)
    {
        return context switch
        {
            BlockExpressionContext c => BindBlockExpression(c, diagnostics),
            NonBlockExpressionContext c => BindExpression(c.Expression, diagnostics),
            _ => throw new UnreachableException()
        };
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

    public BoundExpression BindExpression(ExpressionContext context, DiagnosticList diagnostics)
    {
        return context switch
        {
            LiteralExpressionContext c => BindLiteral(c.Literal, diagnostics),
            NameExpressionContext c => BindNameExpression(c, diagnostics),
            FactorExpressionContext c => BindBinaryExpression(c, c.Left, c.Operator, c.Right, diagnostics),
            TermExpressionContext c => BindBinaryExpression(c, c.Left, c.Operator, c.Right, diagnostics),
            ComparisonExpressionContext c => BindBinaryExpression(c, c.Left, c.Operator, c.Right, diagnostics),
            AssignmentExpressionContext c => BindAssignmentExpression(c, diagnostics),
            _ => throw new UnreachableException()
        };
    }

    private static BoundLiteral BindLiteral(LiteralContext context, DiagnosticList diagnostics)
    {
        (object value, TypeSymbol type) = context switch
        {
            IntegerLiteralContext c => ((object)int.Parse(c.Value.Text.Replace("_", "")), TypeSymbol.Int32),
            TrueLiteralContext => (true, TypeSymbol.Bool),
            FalseLiteralContext => (false, TypeSymbol.Bool),
            _ => throw new UnreachableException()
        };

        return new BoundLiteral(context, value, type);
    }

    private BoundNameExpression BindNameExpression(NameExpressionContext context, DiagnosticList diagnostics)
    {
        var referencedSymbol = Lookup(context.Name.Text);
        if (referencedSymbol == null)
        {
            diagnostics.Add(context.SourceInterval, DiagnosticMessages.NameNotFound(context.Name.Text));
            referencedSymbol = Symbol.Missing;
        }

        return new BoundNameExpression(context, referencedSymbol);
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
            Star => BoundBinaryOperatorKind.Multiplication,
            Slash => BoundBinaryOperatorKind.Division,
            Percentage => BoundBinaryOperatorKind.Remainder,
            Plus => BoundBinaryOperatorKind.Addition,
            Minus => BoundBinaryOperatorKind.Subtraction,
            GreaterThan => BoundBinaryOperatorKind.GreaterThan,
            GreaterThanOrEqual => BoundBinaryOperatorKind.GreaterThanOrEqual,
            LessThan => BoundBinaryOperatorKind.LessThan,
            LessThanOrEqual => BoundBinaryOperatorKind.LessThanOrEqual,
            _ => throw new UnreachableException()
        };

        if (!BoundBinaryOperator.TryBind(binaryOpKind, boundLeft.Type, out var boundOp))
        {
            diagnostics.Add(
                context.SourceInterval,
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
        var value = BindExpression(context.Value, diagnostics);
        if (assignee is not BoundNameExpression name)
        {
            diagnostics.Add(assignee.Context.SourceInterval, DiagnosticMessages.ExpressionIsNotAssignable);
            return new BoundAssignmentExpression(context, Symbol.Missing, value);
        }

        if (name.ReferencedSymbol is not SourceLocalSymbol local)
        {
            diagnostics.Add(assignee.Context.SourceInterval, DiagnosticMessages.SymbolIsNotAssignable);
            return new BoundAssignmentExpression(context, Symbol.Missing, value);
        }

        TypeCheck(context, local.Type, value.Type, diagnostics);
        return new BoundAssignmentExpression(context, local, value);
    }
}
