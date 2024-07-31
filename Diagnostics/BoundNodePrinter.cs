using System.CodeDom.Compiler;
using System.Diagnostics;
using Ca21.Binding;

namespace Ca21.Diagnostics;

internal sealed class BoundNodePrinter(TextWriter writer)
{
    public static readonly BoundNodePrinter ConsolePrinter = new(Console.Out);

    private readonly IndentedTextWriter _writer = new(writer);

    public void PrettyPrint(BoundStatement statement)
    {
        WalkStatement(statement);
        _writer.Flush();
    }

    private void WalkStatement(BoundStatement node)
    {
        switch (node)
        {
            case BoundControlBlockStartStatement n:
                WalkControlBlockStartStatement(n);
                break;
            case BoundControlBlockEndStatement n:
                WalkControlBlockEndStatement(n);
                break;
            case BoundGotoStatement n:
                WalkGotoStatement(n);
                break;
            case BoundConditionalGotoStatement n:
                WalkConditionalGotoStatement(n);
                break;
            case BoundLocalDeclaration n:
                WalkLocalDeclaration(n);
                break;
            case BoundWhileStatement n:
                WalkWhileStatement(n);
                break;
            case BoundReturnStatement n:
                WalkReturnStatement(n);
                break;
            case BoundBlock n:
                WalkBlock(n);
                break;
            case BoundExpressionStatement n:
                WalkExpressionStatement(n);
                break;
            default:
                throw new UnreachableException();
        }
    }

    private void WalkWhileStatement(BoundWhileStatement node)
    {
        _writer.Write(nameof(BoundWhileStatement));
        _writer.Write(' ');
        _writer.WriteLine('{');
        _writer.Indent++;

        _writer.Write(nameof(node.Condition));
        _writer.Write('=');
        WalkExpression(node.Condition);
        _writer.WriteLine();

        _writer.Write(nameof(node.Body));
        _writer.Write('=');
        WalkBlock(node.Body);

        _writer.Indent--;
        _writer.WriteLine('}');
    }

    private void WalkExpressionStatement(BoundExpressionStatement node)
    {
        _writer.Write(nameof(BoundExpressionStatement));
        _writer.Write(' ');
        _writer.Write(nameof(node.Expression));
        _writer.Write('=');
        WalkExpression(node.Expression);
        _writer.WriteLine();
    }

    private void WalkBlock(BoundBlock node)
    {
        _writer.Write(nameof(BoundBlock));
        _writer.Write(' ');
        _writer.WriteLine('{');
        _writer.Indent++;

        foreach (var statement in node.Statements)
            WalkStatement(statement);

        _writer.Indent--;
        _writer.WriteLine('}');
    }

    private void WalkReturnStatement(BoundReturnStatement node)
    {
        _writer.Write(nameof(BoundReturnStatement));
        _writer.Write(' ');
        if (node.Value != null)
            WalkExpression(node.Value);

        _writer.WriteLine();
    }

    private void WalkLocalDeclaration(BoundLocalDeclaration node)
    {
        _writer.Write(nameof(BoundLocalDeclaration));
        _writer.Write(' ');
        _writer.WriteLine('{');
        _writer.Indent++;

        _writer.Write(nameof(node.Local));
        _writer.Write('=');
        _writer.WriteLine(node.Local.Name);

        if (node.Initializer != null)
        {
            _writer.Write(nameof(node.Initializer));
            _writer.Write('=');
            WalkExpression(node.Initializer);
            _writer.WriteLine();
        }

        _writer.Indent--;
        _writer.WriteLine('}');
    }

    private void WalkConditionalGotoStatement(BoundConditionalGotoStatement node)
    {
        _writer.Write(nameof(BoundConditionalGotoStatement));
        _writer.Write(' ');
        _writer.Write('@');
        _writer.Write(node.Target.Name);
        _writer.Write(' ');
        _writer.WriteLine('{');
        _writer.Indent++;

        _writer.Write(nameof(node.Condition));
        _writer.Write('=');
        WalkExpression(node.Condition);
        _writer.WriteLine();

        _writer.Write(nameof(node.BranchIfFalse));
        _writer.Write('=');
        _writer.WriteLine(node.BranchIfFalse);

        _writer.Indent--;
        _writer.WriteLine('}');
    }

    private void WalkGotoStatement(BoundGotoStatement node)
    {
        _writer.Write(nameof(BoundGotoStatement));
        _writer.Write(' ');
        _writer.Write('@');
        _writer.WriteLine(node.Target.Name);
    }

    private void WalkControlBlockStartStatement(BoundControlBlockStartStatement node)
    {
        _writer.Write(nameof(BoundControlBlockStartStatement));
        _writer.Write(' ');
        _writer.Write('@');
        _writer.Write(node.ControlBlockIdentifier.Name);
        _writer.Write(' ');
        _writer.Write(nameof(node.IsLoop));
        _writer.Write('=');
        _writer.WriteLine(node.IsLoop);
    }

    private void WalkControlBlockEndStatement(BoundControlBlockEndStatement node)
    {
        _writer.Write(nameof(BoundControlBlockEndStatement));
        _writer.Write(' ');
        _writer.Write('@');
        _writer.WriteLine(node.ControlBlockIdentifier.Name);
    }

    private void WalkExpression(BoundExpression node)
    {
        switch (node)
        {
            case BoundBlockExpression n:
                WalkBlockExpression(n);
                break;
            case BoundLiteralExpression n:
                WalkLiteral(n);
                break;
            case BoundNameExpression n:
                WalkNameExpression(n);
                break;
            case BoundBinaryExpression n:
                WalkBinaryExpression(n);
                break;
            case BoundAssignmentExpression n:
                WalkAssignmentExpression(n);
                break;
            default:
                throw new UnreachableException();
        }
    }

    private void WalkAssignmentExpression(BoundAssignmentExpression node)
    {
        _writer.Write(nameof(BoundAssignmentExpression));
        _writer.Write(' ');
        _writer.WriteLine('{');
        _writer.Indent++;

        _writer.Write(nameof(node.Assignee));
        _writer.Write('=');
        _writer.WriteLine(node.Assignee.Name);

        _writer.Write(nameof(node.Value));
        _writer.Write('=');
        WalkExpression(node.Value);
        _writer.WriteLine();

        _writer.Indent--;
        _writer.Write('}');
    }

    private void WalkBinaryExpression(BoundBinaryExpression node)
    {
        _writer.Write(nameof(BoundBinaryExpression));
        _writer.Write(' ');
        _writer.WriteLine('{');
        _writer.Indent++;

        _writer.Write(nameof(node.Left));
        _writer.Write('=');
        WalkExpression(node.Left);
        _writer.WriteLine();

        _writer.Write(nameof(node.Operator));
        _writer.Write('=');
        _writer.WriteLine(node.Operator.Kind);

        _writer.Write(nameof(node.Right));
        _writer.Write('=');
        WalkExpression(node.Right);
        _writer.WriteLine();

        _writer.Indent--;
        _writer.Write('}');
    }

    private void WalkNameExpression(BoundNameExpression node)
    {
        _writer.Write(nameof(BoundNameExpression));
        _writer.Write(' ');
        _writer.Write(node.ReferencedSymbol.Name);
    }

    private void WalkLiteral(BoundLiteralExpression node)
    {
        _writer.Write(nameof(BoundLiteralExpression));
        _writer.Write(' ');
        _writer.Write(node.Value);
    }

    private void WalkBlockExpression(BoundBlockExpression node)
    {
        _writer.Write(nameof(BoundBlockExpression));
        _writer.Write(' ');
        _writer.WriteLine('{');
        _writer.Indent++;

        foreach (var statement in node.Statements)
            WalkStatement(statement);

        if (node.TailExpression != null)
        {
            _writer.Write(nameof(node.TailExpression));
            _writer.Write('=');
            WalkExpression(node.TailExpression);
            _writer.WriteLine();
        }

        _writer.Indent--;
        _writer.Write('}');
    }
}
