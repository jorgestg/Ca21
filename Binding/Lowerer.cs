using System.Collections.Immutable;

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

    private static BoundStatement LowerWhileStatement(BoundWhileStatement whileStatement)
    {
        var loweredCondition = LowerExpression(whileStatement.Condition);
        if (loweredCondition.ConstantValue.HasValue && loweredCondition.ConstantValue.Value is false)
            return new BoundNopStatement(whileStatement.Context);

        // continue: jumpIfFalse <condition> break
        // <body>
        // goto continue
        // break: ...
        var statements = new ArrayBuilder<BoundStatement>(whileStatement.Body.Statements.Length + 4);
        statements.Add(new BoundLabelStatement(whileStatement.Context, whileStatement.ContinueLabel));

        BoundStatement goToBreak;
        if (loweredCondition.ConstantValue.HasValue && loweredCondition.ConstantValue.Value is true)
        {
            goToBreak = new BoundGotoStatement(whileStatement.Condition.Context, whileStatement.BreakLabel);
        }
        else
        {
            goToBreak = new BoundConditionalGotoStatement(
                whileStatement.Condition.Context,
                whileStatement.Condition,
                whileStatement.BreakLabel,
                branchIfFalse: true
            );
        }

        statements.Add(goToBreak);

        LowerStatementsToBuilder(whileStatement.Body.Statements, ref statements);
        statements.Add(new BoundGotoStatement(whileStatement.Context, whileStatement.ContinueLabel));
        statements.Add(new BoundLabelStatement(whileStatement.Context, whileStatement.BreakLabel));
        return new BoundBlock(whileStatement.Context, statements.MoveToImmutable());
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
        return expression.ConstantValue.HasValue
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
