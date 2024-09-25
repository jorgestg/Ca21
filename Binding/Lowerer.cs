using System.Collections.Immutable;
using Ca21.Symbols;

namespace Ca21.Binding;

internal static class Lowerer
{
    public static BoundBlock Lower(BoundBlock block)
    {
        var statements = default(ArrayBuilder<BoundStatement>);
        for (var i = 0; i < block.Statements.Length; i++)
        {
            var statement = block.Statements[i];
            var loweredStatement = LowerStatement(statement);
            if (loweredStatement != statement && statements.IsDefault)
            {
                statements = new ArrayBuilder<BoundStatement>(block.Statements.Length);
                for (var j = 0; j < i; j++)
                    statements.Add(block.Statements[j]);
            }

            statements.TryAdd(loweredStatement);
        }

        var result = statements.IsDefault ? block : new BoundBlock(block.Context, statements.MoveToImmutable());
        return Flatten(result);
    }

    private static BoundBlock Flatten(BoundBlock block)
    {
        var statements = default(ArrayBuilder<BoundStatement>);
        FlattenCore(block, ref statements);
        return statements.IsDefault ? block : new BoundBlock(block.Context, statements.DrainToImmutable());

        static void FlattenCore(BoundBlock block, ref ArrayBuilder<BoundStatement> statements)
        {
            for (var i = 0; i < block.Statements.Length; i++)
            {
                var statement = block.Statements[i];
                if (statement.Kind != BoundNodeKind.Block)
                {
                    statements.TryAdd(statement);
                    continue;
                }

                if (statements.IsDefault)
                {
                    statements = new ArrayBuilder<BoundStatement>();
                    for (var j = 0; j < i; j++)
                        statements.Add(block.Statements[j]);
                }

                FlattenCore((BoundBlock)statement, ref statements);
            }
        }
    }

    private static BoundStatement LowerStatement(BoundStatement statement)
    {
        return statement.Kind switch
        {
            BoundNodeKind.Block => Lower((BoundBlock)statement),
            BoundNodeKind.LocalDeclaration => LowerLocalDeclaration((BoundLocalDeclaration)statement),
            BoundNodeKind.IfStatement => LowerIfStatement((BoundIfStatement)statement),
            BoundNodeKind.WhileStatement => LowerWhileStatement((BoundWhileStatement)statement),
            BoundNodeKind.ReturnStatement => LowerReturnStatement((BoundReturnStatement)statement),
            _ => statement,
        };
    }

    private static BoundStatement LowerLocalDeclaration(BoundLocalDeclaration localDeclaration)
    {
        var loweredInitializer =
            localDeclaration.Initializer == null ? null : LowerExpression(localDeclaration.Initializer);

        if (loweredInitializer?.Kind == BoundNodeKind.BlockExpression)
        {
            return LowerBlockExpression(
                localDeclaration,
                (BoundBlockExpression)loweredInitializer,
                static (originalStatement, tail) =>
                {
                    var localDeclaration = (BoundLocalDeclaration)originalStatement;
                    return new BoundLocalDeclaration(localDeclaration.Context, localDeclaration.Local, tail);
                }
            );
        }

        return loweredInitializer == localDeclaration.Initializer
            ? localDeclaration
            : new BoundLocalDeclaration(localDeclaration.Context, localDeclaration.Local, loweredInitializer);
    }

    private static BoundStatement LowerIfStatement(BoundIfStatement statement)
    {
        var loweredCondition = LowerExpression(statement.Condition);
        if (loweredCondition.ConstantValue.HasValue && loweredCondition.ConstantValue.Value is false)
        {
            if (statement.ElseClause == null)
                return new BoundNopStatement(statement.Context);

            return LowerStatement(statement.ElseClause);
        }

        var statements = default(ArrayBuilder<BoundStatement>);
        if (loweredCondition.ConstantValue.HasValue && loweredCondition.ConstantValue.Value is true)
        {
            statements = new ArrayBuilder<BoundStatement>(statement.Body.Statements.Length);
            LowerStatementsToBuilder(statement.Body.Statements, ref statements);
            return new BoundBlock(statement.Context, statements.MoveToImmutable());
        }

        // gotoIfFalse <condition> {else|end}
        // <then>
        // goto end
        // [
        //  else:
        //  <else>
        // ]
        // end:
        var statementCount = statement.Body.Statements.Length + 3;
        if (statement.ElseClause != null)
            statementCount += statement.ElseClause.Statements.Length + 1;

        statements = new ArrayBuilder<BoundStatement>(statementCount);

        var elseClauseLabel = statement.ElseClause == null ? null : new LabelSymbol(statement.Context, "else");
        var endLabel = new LabelSymbol(statement.Context, "end");
        statements.Add(
            new BoundConditionalGotoStatement(
                statement.Condition.Context,
                statement.Condition,
                elseClauseLabel ?? endLabel,
                branchIfFalse: true
            )
        );

        LowerStatementsToBuilder(statement.Body.Statements, ref statements);
        statements.Add(new BoundGotoStatement(statement.Context, endLabel));

        if (statement.ElseClause != null)
        {
            statements.Add(new BoundLabelStatement(statement.Context, elseClauseLabel!));
            LowerStatementsToBuilder(statement.ElseClause.Statements, ref statements);
        }

        statements.Add(new BoundLabelStatement(statement.Context, endLabel));
        return new BoundBlock(statement.Context, statements.MoveToImmutable());
    }

    private static BoundStatement LowerWhileStatement(BoundWhileStatement statement)
    {
        var loweredCondition = LowerExpression(statement.Condition);
        if (loweredCondition.ConstantValue.HasValue && loweredCondition.ConstantValue.Value is false)
            return new BoundNopStatement(statement.Context);

        // continue: gotoIfFalse <condition> break
        // <body>
        // goto continue
        // break: ...
        var statements = new ArrayBuilder<BoundStatement>(statement.Body.Statements.Length + 4);
        statements.Add(new BoundLabelStatement(statement.Context, statement.ContinueLabel));

        BoundStatement goToBreak;
        if (loweredCondition.ConstantValue.HasValue && loweredCondition.ConstantValue.Value is true)
        {
            goToBreak = new BoundGotoStatement(statement.Condition.Context, statement.BreakLabel);
        }
        else
        {
            goToBreak = new BoundConditionalGotoStatement(
                statement.Condition.Context,
                statement.Condition,
                statement.BreakLabel,
                branchIfFalse: true
            );
        }

        statements.Add(goToBreak);

        LowerStatementsToBuilder(statement.Body.Statements, ref statements);
        statements.Add(new BoundGotoStatement(statement.Context, statement.ContinueLabel));
        statements.Add(new BoundLabelStatement(statement.Context, statement.BreakLabel));
        return new BoundBlock(statement.Context, statements.MoveToImmutable());
    }

    private static BoundStatement LowerReturnStatement(BoundReturnStatement returnStatement)
    {
        var loweredValue = returnStatement.Value == null ? null : LowerExpression(returnStatement.Value);
        if (loweredValue?.Kind == BoundNodeKind.BlockExpression)
        {
            return LowerBlockExpression(
                returnStatement,
                (BoundBlockExpression)loweredValue,
                static (originalStatement, tail) => new BoundReturnStatement(originalStatement.Context, tail)
            );
        }

        return loweredValue == returnStatement.Value
            ? returnStatement
            : new BoundReturnStatement(returnStatement.Context, loweredValue);
    }

    private static BoundBlock LowerBlockExpression(
        BoundStatement originalStatement,
        BoundBlockExpression blockExpression,
        Func<BoundStatement, BoundExpression?, BoundStatement> statementFactory
    )
    {
        var statements = new ArrayBuilder<BoundStatement>(blockExpression.Statements.Length + 1);
        LowerStatementsToBuilder(blockExpression.Statements, ref statements);
        var tail = blockExpression.TailExpression == null ? null : LowerExpression(blockExpression.TailExpression);
        statements.Add(statementFactory(originalStatement, tail));
        return new BoundBlock(originalStatement.Context, statements.MoveToImmutable());
    }

    private static BoundExpression LowerExpression(BoundExpression expression)
    {
        return expression.Kind != BoundNodeKind.LiteralExpression && expression.ConstantValue.HasValue
            ? new BoundLiteralExpression(expression.Context, expression.ConstantValue.Value, expression.Type)
            : expression;
    }

    private static void LowerStatementsToBuilder(
        ImmutableArray<BoundStatement> statements,
        ref ArrayBuilder<BoundStatement> builder
    )
    {
        foreach (var statement in statements)
        {
            var lowerStatement = LowerStatement(statement);
            builder.Add(lowerStatement);
        }
    }
}
