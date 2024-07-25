using System.Collections.Immutable;
using System.Diagnostics;
using Ca21.Symbols;

namespace Ca21.Binding;

internal sealed class Lowerer
{
    public static BoundBlock Lower(BoundBlock block)
    {
        ImmutableArray<BoundStatement>.Builder? statements = null;
        for (var i = 0; i < block.Statements.Length; i++)
        {
            var statement = block.Statements[i];
            var loweredStatement = LowerStatement(statement);
            if (loweredStatement != statement && statements == null)
            {
                statements = ImmutableArray.CreateBuilder<BoundStatement>(block.Statements.Length);
                for (var j = 0; j < i; j++)
                    statements.Add(block.Statements[j]);
            }

            statements?.Add(loweredStatement);
        }

        var result = statements == null ? block : new BoundBlock(block.Context, statements.MoveToImmutable());
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
        var loweredInitializer = LowerExpression(localDeclaration.Initializer);
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

    private static BoundBlock LowerWhileStatement(BoundWhileStatement whileStatement)
    {
        // goto continue
        // body:
        //   <body>
        // continue:
        //   jumpIfTrue <condition> body
        // break:
        //   ...
        var statements = ImmutableArray.CreateBuilder<BoundStatement>(whileStatement.Body.Statements.Length + 5);
        statements.Add(new BoundGotoStatement(whileStatement.Context, whileStatement.ContinueLabel));

        var bodyLabel = new LabelSymbol(whileStatement.Body.Context, "body");
        statements.Add(new BoundLabelDeclarationStatement(whileStatement.Context, bodyLabel));

        foreach (var statement in whileStatement.Body.Statements)
        {
            var loweredStatement = LowerStatement(statement);
            statements.Add(loweredStatement);
        }

        statements.Add(new BoundLabelDeclarationStatement(whileStatement.Context, whileStatement.ContinueLabel));
        statements.Add(
            new BoundConditionalGotoStatement(
                whileStatement.Context,
                whileStatement.Condition,
                bodyLabel,
                whileStatement.BreakLabel
            )
        );

        statements.Add(new BoundLabelDeclarationStatement(whileStatement.Context, whileStatement.BreakLabel));
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
        Func<BoundStatement, BoundExpression, BoundStatement> statementFactory
    )
    {
        Debug.Assert(blockExpression.TailExpression != null);
        var tail = LowerExpression(blockExpression.TailExpression!);
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
