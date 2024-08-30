using System.Collections.Immutable;
using System.Diagnostics;
using Ca21.Symbols;

namespace Ca21.Binding;

internal sealed class ControlFlowGraph
{
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

    private ControlFlowGraph(
        BoundBlock body,
        BasicBlock entry,
        BasicBlock exit,
        List<BasicBlock> blocks,
        List<BasicBlockEdge> edges
    )
    {
        Body = body;
        Entry = entry;
        Exit = exit;
        Blocks = blocks;
        Edges = edges;
    }

    private BasicBlock Entry { get; }
    private BasicBlock Exit { get; }
    private List<BasicBlock> Blocks { get; }
    private List<BasicBlockEdge> Edges { get; }

    public BoundBlock Body { get; }

    public bool AllPathsReturn()
    {
        foreach (var edge in Exit.Incoming)
        {
            var lastStatement = edge.From.Statements.LastOrDefault();
            if (lastStatement?.Kind != BoundNodeKind.ReturnStatement)
                return false;
        }

        return true;
    }

    public ImmutableArray<BoundStatement> GetReachableStatements(
        out ImmutableArray<BoundStatement> unreachableStatements
    )
    {
        if (Blocks.Sum(block => block.Statements.Count) == Body.Statements.Length)
        {
            unreachableStatements = [];
            return Body.Statements;
        }

        var reachableStatementSet = new HashSet<BoundStatement>(Body.Statements.Length);
        foreach (var block in Blocks)
        {
            foreach (var statement in block.Statements)
                reachableStatementSet.Add(statement);
        }

        var unreachableStatementCount = Body.Statements.Length - reachableStatementSet.Count;
        var unreachableStatementsBuilder = new ArrayBuilder<BoundStatement>(unreachableStatementCount);
        var reachableStatements = new ArrayBuilder<BoundStatement>(reachableStatementSet.Count);
        foreach (var statement in Body.Statements)
        {
            if (reachableStatementSet.Contains(statement))
            {
                reachableStatements.Add(statement);
                continue;
            }

            unreachableStatementsBuilder.Add(statement);
        }

        unreachableStatements = unreachableStatementsBuilder.MoveToImmutable();
        return reachableStatements.MoveToImmutable();
    }

    public static ControlFlowGraph Create(BoundBlock body)
    {
        var basicBlocks = CreateBasicBlocks(body);
        return ConnectBlocks(body, basicBlocks);
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
        var edges = new List<BasicBlockEdge>();
        var entryBlock = new BasicBlock();
        var exitBlock = new BasicBlock();

        if (basicBlocks.Count == 0)
        {
            edges.Capacity = 1;
            Connect(edges, entryBlock, exitBlock);

            basicBlocks.Capacity = 2;
            basicBlocks.Add(entryBlock);
            basicBlocks.Add(exitBlock);

            return new ControlFlowGraph(body, entryBlock, exitBlock, basicBlocks, edges);
        }

        Connect(edges, entryBlock, basicBlocks[0]);

        var idToBasicBlock = new Dictionary<BoundLabel, BasicBlock>();
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
                    Connect(edges, currentBlock, targetBlock);
                    Connect(edges, currentBlock, nextBlock);
                    break;
                }

                case BoundNodeKind.ReturnStatement:
                {
                    Connect(edges, currentBlock, exitBlock);
                    break;
                }

                case BoundNodeKind.GotoStatement:
                {
                    var gotoStatement = (BoundGotoStatement)lastStatement;
                    var targetBlock = idToBasicBlock[gotoStatement.Target];
                    Connect(edges, currentBlock, targetBlock);
                    break;
                }

                case BoundNodeKind.NopStatement:
                case BoundNodeKind.LabelStatement:
                case BoundNodeKind.LocalDeclaration:
                case BoundNodeKind.ExpressionStatement:
                {
                    Connect(edges, currentBlock, nextBlock);
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
            {
                edge.To.Incoming.Remove(edge);
                edges.Remove(edge);
            }

            basicBlocks.Remove(basicBlock);

            // Scan again because the collection changed
            i = -1;
        }

        basicBlocks.Insert(0, entryBlock);
        basicBlocks.Add(exitBlock);

        return new ControlFlowGraph(body, entryBlock, exitBlock, basicBlocks, edges);

        static void Connect(List<BasicBlockEdge> edges, BasicBlock from, BasicBlock to)
        {
            var edge = new BasicBlockEdge(from, to);
            from.Outgoing.Add(edge);
            to.Incoming.Add(edge);
            edges.Add(edge);
        }
    }
}
