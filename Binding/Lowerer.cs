using System.Collections.Immutable;
using Ca21.Symbols;

namespace Ca21.Binding;

internal sealed class Lowerer
{
    public static BoundBlock Lower(BoundBlock block, bool lowerControlFlow)
    {
        ImmutableArray<BoundStatement>.Builder? statements = null;
        foreach (var statement in block.Statements)
        {
            var loweredStatement = LowerStatement(statement, lowerControlFlow);
            if (loweredStatement != statement)
                statements ??= ImmutableArray.CreateBuilder<BoundStatement>();

            statements?.Add(loweredStatement);
        }

        var result = statements == null ? block : new BoundBlock(block.Context, statements.DrainToImmutable());
        return Flatten(result);
    }

    private static BoundBlock Flatten(BoundBlock block)
    {
        ImmutableArray<BoundStatement>.Builder? statements = null;
        FlattenCore(block, ref statements);
        return statements == null ? block : new BoundBlock(block.Context, statements.DrainToImmutable());

        static void FlattenCore(BoundBlock block, ref ImmutableArray<BoundStatement>.Builder? statements)
        {
            foreach (var statement in block.Statements)
            {
                if (statement is BoundBlock b)
                {
                    statements ??= ImmutableArray.CreateBuilder<BoundStatement>();
                    FlattenCore(b, ref statements);
                    continue;
                }

                statements?.Add(statement);
            }
        }
    }

    private static BoundStatement LowerStatement(BoundStatement statement, bool lowerControlFlow)
    {
        return statement switch
        {
            BoundLocalDeclaration d => LowerLocalDeclaration(d, lowerControlFlow),
            BoundWhileStatement w => LowerWhileStatement(w, lowerControlFlow),
            BoundReturnStatement r => LowerReturnStatement(r, lowerControlFlow),
            BoundBlock b => Lower(b, lowerControlFlow),
            _ => statement,
        };
    }

    private static BoundStatement LowerLocalDeclaration(BoundLocalDeclaration localDeclaration, bool lowerControlFlow)
    {
        if (localDeclaration.Initializer is not BoundBlockExpression block)
            return localDeclaration;

        var statements = ImmutableArray.CreateBuilder<BoundStatement>(block.Statements.Length + 1);
        foreach (var statement in block.Statements)
        {
            var loweredStatement = LowerStatement(statement, lowerControlFlow);
            statements.Add(loweredStatement);
        }

        statements.Add(
            new BoundLocalDeclaration(localDeclaration.Context, localDeclaration.Local, block.TailExpression!)
        );

        return new BoundBlock(localDeclaration.Context, statements.MoveToImmutable());
    }

    private static BoundStatement LowerWhileStatement(BoundWhileStatement whileStatement, bool lowerControlFlow)
    {
        if (!lowerControlFlow)
        {
            var loweredBody = Lower(whileStatement.Body, lowerControlFlow);
            if (loweredBody == whileStatement.Body)
                return whileStatement;

            return new BoundWhileStatement(
                whileStatement.Context,
                whileStatement.Condition,
                loweredBody,
                whileStatement.ContinueLabel,
                whileStatement.BreakLabel
            );
        }

        // goto continue
        // body:
        //   <body>
        // continue:
        //   jumpIfTrue <condition> body
        // break:
        //   ...
        var statements = ImmutableArray.CreateBuilder<BoundStatement>(whileStatement.Body.Statements.Length + 5);
        statements.Add(new BoundGotoStatement(whileStatement.Context, whileStatement.ContinueLabel));

        var bodyLabel = new LabelSymbol();
        statements.Add(new BoundLabelStatement(whileStatement.Context, bodyLabel));

        foreach (var statement in whileStatement.Body.Statements)
        {
            var loweredStatement = LowerStatement(statement, lowerControlFlow);
            statements.Add(loweredStatement);
        }

        statements.Add(new BoundLabelStatement(whileStatement.Context, whileStatement.ContinueLabel));
        statements.Add(new BoundConditionalGotoStatement(whileStatement.Context, whileStatement.Condition, bodyLabel));
        statements.Add(new BoundLabelStatement(whileStatement.Context, whileStatement.BreakLabel));
        return new BoundBlock(whileStatement.Context, statements.MoveToImmutable());
    }

    private static BoundStatement LowerReturnStatement(BoundReturnStatement returnStatement, bool lowerControlFlow)
    {
        if (returnStatement.Value is not BoundBlockExpression block)
            return returnStatement;

        var statements = ImmutableArray.CreateBuilder<BoundStatement>(block.Statements.Length + 1);
        foreach (var statement in block.Statements)
        {
            var loweredStatement = LowerStatement(statement, lowerControlFlow);
            statements.Add(loweredStatement);
        }

        statements.Add(new BoundReturnStatement(returnStatement.Context, block.TailExpression!));
        return new BoundBlock(returnStatement.Context, statements.MoveToImmutable());
    }
}
