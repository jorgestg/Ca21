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
            var lastStatement = edge.From.Statements.Last();
            if (lastStatement is not BoundReturnStatement)
                return false;
        }

        return true;
    }

    public static ControlFlowGraph Create(BoundBlock block)
    {
        var loweredBlock = Lowerer.Lower(block, lowerControlFlow: true);
        var basicBlocks = CreateBasicBlocks(loweredBlock);
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
                case BoundLabelStatement:
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
            var edge = Connect(entryBlock, exitBlock);
            edges.Capacity = 1;
            edges.Add(edge);
            return new ControlFlowGraph(entryBlock, exitBlock, basicBlocks, edges);
        }

        var labelToBlock = new Dictionary<LabelSymbol, BasicBlock>();
        foreach (var basicBlock in basicBlocks)
        {
            foreach (var statement in basicBlock.Statements)
            {
                if (statement is BoundLabelStatement labelStatement)
                    labelToBlock.Add(labelStatement.Label, basicBlock);
            }
        }

        for (int i = 0; i < basicBlocks.Count; i++)
        {
            var basicBlock = basicBlocks[i];
            var nextBlock = i + 1 < basicBlocks.Count ? basicBlocks[i + 1] : exitBlock;
            foreach (var statement in basicBlock.Statements)
            {
                switch (statement)
                {
                    case BoundConditionalGotoStatement conditionalGotoStatement:
                    {
                        var targetBlock = labelToBlock[conditionalGotoStatement.Target];
                        var thenEdge = Connect(basicBlock, targetBlock);
                        edges.Add(thenEdge);
                        var elseEdge = Connect(basicBlock, nextBlock);
                        edges.Add(elseEdge);
                        break;
                    }

                    case BoundReturnStatement:
                    {
                        var edge = Connect(basicBlock, exitBlock);
                        edges.Add(edge);
                        break;
                    }

                    case BoundGotoStatement gotoStatement:
                    {
                        var targetBlock = labelToBlock[gotoStatement.Target];
                        var edge = Connect(basicBlock, targetBlock);
                        edges.Add(edge);
                        break;
                    }
                }
            }
        }

        return new ControlFlowGraph(entryBlock, exitBlock, basicBlocks, edges);

        static BasicBlockEdge Connect(BasicBlock from, BasicBlock to)
        {
            var edge = new BasicBlockEdge(from, to);
            from.Outgoing.Add(edge);
            to.Incoming.Add(edge);
            return edge;
        }
    }
}
