using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Ca21.Symbols;

namespace Ca21.Binding;

internal sealed class ControlFlowGraph
{
    private readonly ImmutableArray<BasicBlock> _blocks;

    private ControlFlowGraph(
        BoundBlock originalBody,
        ImmutableArray<BoundStatement> body,
        ImmutableArray<BasicBlock> blocks
    )
    {
        _blocks = blocks;

        OriginalBody = originalBody;
        Statements = body;
    }

    public BoundBlock OriginalBody { get; }
    public ImmutableArray<BoundStatement> Statements { get; }

    public bool AllPathsReturn()
    {
        var exit = _blocks[^1];
        foreach (var edge in exit.Incoming)
        {
            var lastStatement = edge.From.Statements.LastOrDefault();
            if (lastStatement?.Kind != BoundNodeKind.ReturnStatement)
                return false;
        }

        return true;
    }

    public bool IsStartOfLoop(BoundLabelStatement statement)
    {
        foreach (var block in _blocks)
        {
            // We only check the first statement because LabelStatements always start new blocks
            if (block.Statements.Count > 0 && block.Statements[0] == statement)
            {
                if (block.Statements[^1].Kind != BoundNodeKind.GotoStatement)
                    return false;

                var blockStartLabel = ((BoundLabelStatement)block.Statements[0]).Label;
                var blockExitLabel = ((BoundGotoStatement)block.Statements[^1]).Target;
                return blockStartLabel == blockExitLabel;
            }
        }

        throw new ArgumentException(
            "Statement is not inside ControlFlowGraph.Body or is unreachable",
            nameof(statement)
        );
    }

    public ImmutableArray<BoundStatement> GetUnreachableStatements()
    {
        if (Statements.Length == OriginalBody.Statements.Length)
            return [];

        var statementsAsArray = ImmutableCollectionsMarshal.AsArray(Statements)!;
        var statementSet = new HashSet<BoundStatement>(statementsAsArray);
        var unreachableStatementCount = OriginalBody.Statements.Length - statementSet.Count;
        var unreachableStatementsBuilder = new ArrayBuilder<BoundStatement>(unreachableStatementCount);
        foreach (var statement in OriginalBody.Statements)
        {
            if (statementSet.Contains(statement))
                continue;

            unreachableStatementsBuilder.Add(statement);
        }

        return unreachableStatementsBuilder.MoveToImmutable();
    }

    public static ControlFlowGraph Create(BoundBlock body)
    {
        if (body.Statements.Length == 0)
        {
            var entryBlock = new BasicBlock();
            entryBlock.Outgoing.Capacity = 1;

            var exitBlock = new BasicBlock();
            exitBlock.Incoming.Capacity = 1;

            var edge = new BasicBlockEdge(entryBlock, exitBlock);
            entryBlock.Outgoing.Add(edge);
            exitBlock.Incoming.Add(edge);
            return new ControlFlowGraph(body, [], [entryBlock, exitBlock]);
        }

        var basicBlocks = CreateBasicBlocks(body);
        return ConnectBlocks(body, basicBlocks);
    }

    private sealed class BasicBlockEdge(BasicBlock from, BasicBlock to)
    {
        public BasicBlock From { get; } = from;
        public BasicBlock To { get; } = to;
    }

    private sealed class BasicBlock()
    {
        public List<BasicBlockEdge> Incoming { get; } = new();
        public List<BoundStatement> Statements { get; } = new();
        public List<BasicBlockEdge> Outgoing { get; } = new();
    }

    private static List<BasicBlock> CreateBasicBlocks(BoundBlock body)
    {
        var basicBlocks = new List<BasicBlock>();
        var statements = new List<BoundStatement>();
        foreach (var statement in body.Statements)
        {
            switch (statement.Kind)
            {
                case BoundNodeKind.LabelStatement:
                {
                    if (statements.Count > 0)
                    {
                        var basicBlock = new BasicBlock();
                        basicBlock.Statements.AddRange(statements);
                        basicBlocks.Add(basicBlock);
                        statements.Clear();
                    }

                    statements.Add(statement);
                    break;
                }

                // Gotos and returns end blocks
                case BoundNodeKind.ConditionalGotoStatement:
                case BoundNodeKind.ReturnStatement:
                case BoundNodeKind.GotoStatement:
                {
                    statements.Add(statement);
                    var basicBlock = new BasicBlock();
                    basicBlock.Statements.AddRange(statements);
                    basicBlocks.Add(basicBlock);
                    statements.Clear();
                    break;
                }

                case BoundNodeKind.NopStatement:
                case BoundNodeKind.ExpressionStatement:
                case BoundNodeKind.LocalDeclaration:
                {
                    statements.Add(statement);
                    break;
                }

                default:
                    throw new UnreachableException();
            }
        }

        if (statements.Count > 0)
        {
            var basicBlock = new BasicBlock();
            basicBlock.Statements.AddRange(statements);
            basicBlocks.Add(basicBlock);
            statements.Clear();
        }

        return basicBlocks;
    }

    private static ControlFlowGraph ConnectBlocks(BoundBlock body, List<BasicBlock> basicBlocks)
    {
        var entryBlock = new BasicBlock();
        var exitBlock = new BasicBlock();

        if (basicBlocks.Count == 0)
        {
            Connect(entryBlock, exitBlock);
            return new ControlFlowGraph(body, [], [entryBlock, exitBlock]);
        }

        Connect(entryBlock, basicBlocks[0]);

        var idToBasicBlock = new Dictionary<LabelSymbol, BasicBlock>();
        foreach (var basicBlock in basicBlocks)
        {
            var firstStatement = basicBlock.Statements.FirstOrDefault();
            if (firstStatement?.Kind == BoundNodeKind.LabelStatement)
                idToBasicBlock.Add(((BoundLabelStatement)firstStatement).Label, basicBlock);
        }

        for (var i = 0; i < basicBlocks.Count; i++)
        {
            var currentBlock = basicBlocks[i];
            var nextBlock = i + 1 < basicBlocks.Count ? basicBlocks[i + 1] : exitBlock;
            var lastStatement = currentBlock.Statements[^1];
            switch (lastStatement.Kind)
            {
                case BoundNodeKind.ConditionalGotoStatement:
                {
                    var conditionalGotoStatement = (BoundConditionalGotoStatement)lastStatement;
                    var targetBlock = idToBasicBlock[conditionalGotoStatement.Target];
                    Connect(currentBlock, targetBlock);
                    Connect(currentBlock, nextBlock);
                    break;
                }

                case BoundNodeKind.ReturnStatement:
                {
                    Connect(currentBlock, exitBlock);
                    break;
                }

                case BoundNodeKind.GotoStatement:
                {
                    var gotoStatement = (BoundGotoStatement)lastStatement;
                    var targetBlock = idToBasicBlock[gotoStatement.Target];
                    Connect(currentBlock, targetBlock);
                    break;
                }

                case BoundNodeKind.NopStatement:
                case BoundNodeKind.LabelStatement:
                case BoundNodeKind.LocalDeclaration:
                case BoundNodeKind.ExpressionStatement:
                {
                    Connect(currentBlock, nextBlock);
                    break;
                }

                default:
                    throw new UnreachableException();
            }
        }

        // Remove unreachable blocks
        for (var i = 0; i < basicBlocks.Count; i++)
        {
            var basicBlock = basicBlocks[i];
            if (basicBlock.Incoming.Count > 0)
                continue;

            foreach (var edge in basicBlock.Outgoing)
                edge.To.Incoming.Remove(edge);

            basicBlocks.Remove(basicBlock);

            // Scan again because the collection changed
            i = -1;
        }

        basicBlocks.Insert(0, entryBlock);
        basicBlocks.Add(exitBlock);

        var statementCount = basicBlocks.Sum(block => block.Statements.Count);
        var statementsBuilder = new ArrayBuilder<BoundStatement>(statementCount);
        foreach (var block in basicBlocks)
        {
            foreach (var statement in block.Statements)
                statementsBuilder.Add(statement);
        }

        return new ControlFlowGraph(body, statementsBuilder.MoveToImmutable(), basicBlocks.ToImmutableArray());

        static void Connect(BasicBlock from, BasicBlock to)
        {
            var edge = new BasicBlockEdge(from, to);
            from.Outgoing.Add(edge);
            to.Incoming.Add(edge);
        }
    }
}
