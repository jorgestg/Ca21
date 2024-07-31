using System.Collections.Immutable;

namespace Ca21.Binding;

internal static class Lowerer
{
    public static BoundBlock Lower(BoundBlock block)
    {
        ArrayBuilder<BoundStatement> statements = default;
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

            if (!statements.IsDefault)
                statements.Add(loweredStatement);
        }

        var result = statements.IsDefault ? block : new BoundBlock(block.Context, statements.MoveToImmutable());
        return Flatten(result);
    }

    private static BoundBlock Flatten(BoundBlock block)
    {
        ImmutableArray<BoundStatement>.Builder? statements = null;
        FlattenCore(block, ref statements);
        return statements == null ? block : new BoundBlock(block.Context, statements.DrainToImmutable());

        static void FlattenCore(BoundBlock block, ref ImmutableArray<BoundStatement>.Builder? statements)
        {
            for (var i = 0; i < block.Statements.Length; i++)
            {
                var statement = block.Statements[i];
                if (statement is not BoundBlock b)
                {
                    statements?.Add(statement);
                    continue;
                }

                if (statements == null)
                {
                    statements = ImmutableArray.CreateBuilder<BoundStatement>();
                    for (var j = 0; j < i; j++)
                        statements.Add(block.Statements[j]);
                }

                FlattenCore(b, ref statements);
            }
        }
    }

    private static BoundStatement LowerStatement(BoundStatement statement)
    {
        return statement switch
        {
            BoundBlock b => Lower(b),
            BoundLocalDeclaration d => LowerLocalDeclaration(d),
            BoundWhileStatement w => LowerWhileStatement(w),
            BoundReturnStatement r => LowerReturnStatement(r),
            _ => statement,
        };
    }

    private static BoundStatement LowerLocalDeclaration(BoundLocalDeclaration localDeclaration)
    {
        var loweredInitializer =
            localDeclaration.Initializer == null ? null : LowerExpression(localDeclaration.Initializer);

        if (loweredInitializer is BoundBlockExpression blockExpression)
        {
            return LowerBlockExpression(
                localDeclaration,
                blockExpression,
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
        if (loweredCondition.ConstantValue.HasValue && (bool)loweredCondition.ConstantValue.Value == false)
            return new BoundNopStatement(whileStatement.Context);

        // block $break
        //   loop $loop
        //     <check>
        //     br.false $break
        //     <code>
        //     br $loop
        //   end
        // end
        var statements = new ArrayBuilder<BoundStatement>(whileStatement.Body.Statements.Length + 6);
        var breakBlock = new BoundControlBlockStartStatement(whileStatement.Context, whileStatement.BreakIdentifier);
        var loopBlock = new BoundControlBlockStartStatement(
            whileStatement.Context,
            whileStatement.ContinueIdentifier,
            isLoop: true
        );

        var check = new BoundConditionalGotoStatement(
            whileStatement.Context,
            whileStatement.Condition,
            whileStatement.BreakIdentifier,
            branchIfFalse: true
        );

        statements.Add(breakBlock);
        statements.Add(loopBlock);
        statements.Add(check);

        foreach (var statement in whileStatement.Body.Statements)
        {
            var loweredStatement = LowerStatement(statement);
            statements.Add(loweredStatement);
        }

        var gotoLoop = new BoundGotoStatement(whileStatement.Context, whileStatement.ContinueIdentifier);
        var loopEnd = new BoundControlBlockEndStatement(whileStatement.Context, whileStatement.ContinueIdentifier);
        var exitEnd = new BoundControlBlockEndStatement(whileStatement.Context, whileStatement.BreakIdentifier);
        statements.Add(gotoLoop);
        statements.Add(loopEnd);
        statements.Add(exitEnd);

        return new BoundBlock(whileStatement.Context, statements.MoveToImmutable());
    }

    private static BoundStatement LowerReturnStatement(BoundReturnStatement returnStatement)
    {
        var loweredValue = returnStatement.Value == null ? null : LowerExpression(returnStatement.Value);
        if (loweredValue is BoundBlockExpression blockExpression)
        {
            return LowerBlockExpression(
                returnStatement,
                blockExpression,
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
        var tail = blockExpression.TailExpression == null ? null : LowerExpression(blockExpression.TailExpression);
        var statement = statementFactory(originalStatement, tail);
        return new BoundBlock(originalStatement.Context, [.. blockExpression.Statements, statement]);
    }

    private static BoundExpression LowerExpression(BoundExpression expression)
    {
        return expression.ConstantValue.HasValue
            ? new BoundLiteralExpression(expression.Context, expression.ConstantValue.Value, expression.Type)
            : expression;
    }
}
