using System.Collections.Immutable;
using Ca21.Binding;

namespace Ca21.Diagnostics;

internal sealed class BoundTreePrinter
{
    private readonly TextWriter _writer;

    private BoundTreePrinter(TextWriter writer)
    {
        _writer = writer;
    }

    public static void Print(BoundNode node, TextWriter writer)
    {
        var printer = new BoundTreePrinter(writer);
        printer.Print(node);
    }

    public static void Print<T>(ImmutableArray<T> nodes, TextWriter writer)
        where T : BoundNode
    {
        var printer = new BoundTreePrinter(writer);
        foreach (var node in nodes)
            printer.Print(node);
    }

    private void Print(BoundNode node)
    {
        switch (node.Kind)
        {
            case BoundNodeKind.NopStatement:
                _writer.WriteLine("nop");
                break;
            case BoundNodeKind.LabelStatement:
                _writer.Write("label @");
                _writer.WriteLine(((BoundLabelStatement)node).Label.Name);
                break;
            case BoundNodeKind.GotoStatement:
                _writer.Write("goto @");
                _writer.WriteLine(((BoundGotoStatement)node).Target.Name);
                break;
            case BoundNodeKind.ConditionalGotoStatement:
                var cgs = (BoundConditionalGotoStatement)node;
                _writer.Write(cgs.BranchIfFalse ? "gotoifnot " : "gotoif ");
                Print(cgs.Condition);
                _writer.Write(" @");
                _writer.WriteLine(cgs.Target.Name);
                break;
            case BoundNodeKind.LocalDeclaration:
                var local = (BoundLocalDeclaration)node;
                _writer.Write("let ");
                _writer.Write(local.Local.Name);
                if (local.Initializer != null)
                {
                    _writer.Write(" = ");
                    Print(local.Initializer);
                }

                _writer.WriteLine();
                break;
            case BoundNodeKind.ReturnStatement:
                var ret = (BoundReturnStatement)node;
                _writer.Write("return ");
                if (ret.Expression != null)
                    Print(ret.Expression);

                _writer.WriteLine();
                break;
            case BoundNodeKind.ExpressionStatement:
                var es = (BoundExpressionStatement)node;
                Print(es.Expression);
                _writer.WriteLine();
                break;
            case BoundNodeKind.Block:
                foreach (var statement in ((BoundBlock)node).Statements)
                    Print(statement);
                break;
            case BoundNodeKind.CastExpression:
                var cast = (BoundCastExpression)node;
                _writer.Write("cast(");
                _writer.Write(cast.Type.Name);
                _writer.Write(", ");
                Print(cast.Expression);
                _writer.WriteLine(')');
                break;
            case BoundNodeKind.LiteralExpression:
                _writer.Write(((BoundLiteralExpression)node).Value);
                break;
            case BoundNodeKind.CallExpression:
                var call = (BoundCallExpression)node;
                _writer.Write(call.Function.Name);
                _writer.Write('(');
                var first = true;
                foreach (var arg in call.Arguments)
                {
                    if (first)
                        first = false;
                    else
                        _writer.Write(", ");

                    Print(arg);
                }

                _writer.WriteLine(')');
                break;
            case BoundNodeKind.AccessExpression:
                var access = (BoundAccessExpression)node;
                Print(access.Left);
                _writer.Write('.');
                _writer.Write(access.ReferencedMember.Name);
                break;
            case BoundNodeKind.StructureLiteralExpression:
                break;
            case BoundNodeKind.UnaryExpression:
                var unary = (BoundUnaryExpression)node;
                _writer.Write(unary.Operator.Kind);
                _writer.Write('(');
                Print(unary.Operand);
                _writer.Write(')');
                break;
            case BoundNodeKind.NameExpression:
                _writer.Write(((BoundNameExpression)node).ReferencedSymbol.Name);
                break;
            case BoundNodeKind.BinaryExpression:
                var binary = (BoundBinaryExpression)node;
                _writer.Write(binary.Operator.Kind);
                _writer.Write('(');
                Print(binary.Left);
                _writer.Write(',');
                _writer.Write(' ');
                Print(binary.Right);
                _writer.Write(')');
                break;
            case BoundNodeKind.AssignmentExpression:
                var assignment = (BoundAssignmentExpression)node;
                _writer.Write(assignment.Assignee.Name);
                _writer.Write(" = ");
                Print(assignment.Expression);
                break;
        }
    }
}
