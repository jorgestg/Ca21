using System.Collections.Immutable;
using System.Diagnostics;
using Ca21.Diagnostics;
using Ca21.Symbols;
using static Ca21.Antlr.Ca21Parser;

namespace Ca21.Binding;

internal sealed partial class LocalScopeBinder(Binder parent) : Binder
{
    private Dictionary<string, Symbol>? _locals;

    public override Binder Parent { get; } = parent;

    public override Symbol? Lookup(string name)
    {
        return _locals?.GetValueOrDefault(name) ?? Parent.Lookup(name);
    }

    public void Define(Symbol symbol)
    {
        _locals ??= new Dictionary<string, Symbol>();
        _locals[symbol.Name] = symbol;
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
        Define(local);
        return new BoundLocalDeclaration(context, local, initializer);
    }

    private BoundWhileStatement BindWhileStatement(WhileStatementContext context, DiagnosticList diagnostics)
    {
        var condition = BindExpression(context.Condition, diagnostics);
        TypeCheck(context, TypeSymbol.Bool, condition.Type, diagnostics);
        var body = BindBlock(context.Body, diagnostics);
        var continueIdentifier = new ControlBlockIdentifier("loop");
        var breakIdentifier = new ControlBlockIdentifier("break");
        return new BoundWhileStatement(context, condition, body, continueIdentifier, breakIdentifier);
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
        if (expression is not BoundAssignmentExpression and not BoundCallExpression)
            diagnostics.Add(context, DiagnosticMessages.ExpressionCannotBeUsedAsStatement);

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
}