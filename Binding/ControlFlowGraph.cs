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
        public List<BasicBlockEdge> Incoming { get; set; } = new();
        public List<BoundStatement> Statements { get; } = new();
        public List<BasicBlockEdge> Outgoing { get; set; } = new();
    }

    private ControlFlowGraph(BasicBlock entry, BasicBlock exit, List<BasicBlock> blocks, List<BasicBlockEdge> edges)
    {
        Entry = entry;
        Exit = exit;
        Blocks = blocks;
        Edges = edges;
    }

    private BasicBlock Entry { get; }
    private BasicBlock Exit { get; }
    private List<BasicBlock> Blocks { get; }
    private List<BasicBlockEdge> Edges { get; }

    public bool AllPathsReturn()
    {
        foreach (var edge in Exit.Incoming)
        {
            var lastStatement = edge.From.Statements.LastOrDefault();
            if (lastStatement is not BoundReturnStatement)
                return false;
        }

        return true;
    }

    public static ControlFlowGraph Create(BoundBlock block)
    {
        var basicBlocks = CreateBasicBlocks(block);
        return ConnectBlocks(basicBlocks);
    }

    private static List<BasicBlock> CreateBasicBlocks(BoundBlock block)
    {
        var basicBlocks = new List<BasicBlock>();
        var statements = new List<BoundStatement>();
        foreach (var statement in block.Statements)
        {
            switch (statement)
            {
                // Labels start new basic blocks
                case BoundLabelDeclarationStatement:
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
                case BoundConditionalGotoStatement:
                case BoundReturnStatement:
                case BoundGotoStatement:
                {
                    statements.Add(statement);
                    var basicBlock = new BasicBlock();
                    basicBlock.Statements.AddRange(statements);
                    basicBlocks.Add(basicBlock);
                    statements.Clear();
                    break;
                }

                case BoundExpressionStatement:
                case BoundLocalDeclaration:
                {
                    statements.Add(statement);
                    break;
                }

                default:
                    throw new UnreachableException();
            }
        }

        return basicBlocks;
    }

    private static ControlFlowGraph ConnectBlocks(List<BasicBlock> basicBlocks)
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

            return new ControlFlowGraph(entryBlock, exitBlock, basicBlocks, edges);
        }

        Connect(edges, entryBlock, basicBlocks[0]);

        var labelToBlock = new Dictionary<LabelSymbol, BasicBlock>();
        foreach (var basicBlock in basicBlocks)
        {
            var firstStatement = basicBlock.Statements.FirstOrDefault();
            if (firstStatement is BoundLabelDeclarationStatement labelStatement)
                labelToBlock.Add(labelStatement.Label, basicBlock);
        }

        for (var i = 0; i < basicBlocks.Count; i++)
        {
            var currentBlock = basicBlocks[i];
            var nextBlock = i + 1 < basicBlocks.Count ? basicBlocks[i + 1] : exitBlock;
            var lastStatement = currentBlock.Statements[^1];
            switch (lastStatement)
            {
                case BoundConditionalGotoStatement conditionalGotoStatement:
                {
                    var targetBlock = labelToBlock[conditionalGotoStatement.Then];
                    Connect(edges, currentBlock, targetBlock);
                    Connect(edges, currentBlock, nextBlock);
                    break;
                }

                case BoundReturnStatement:
                {
                    Connect(edges, currentBlock, exitBlock);
                    break;
                }

                case BoundGotoStatement gotoStatement:
                {
                    var targetBlock = labelToBlock[gotoStatement.Target];
                    Connect(edges, currentBlock, targetBlock);
                    break;
                }

                case BoundLabelDeclarationStatement:
                case BoundLocalDeclaration:
                case BoundExpressionStatement:
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

        return new ControlFlowGraph(entryBlock, exitBlock, basicBlocks, edges);

        static void Connect(List<BasicBlockEdge> edges, BasicBlock from, BasicBlock to)
        {
            var edge = new BasicBlockEdge(from, to);
            from.Outgoing.Add(edge);
            to.Incoming.Add(edge);
            edges.Add(edge);
        }
    }
}
