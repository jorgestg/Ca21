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
                var ld = (BoundLocalDeclaration)node;
                _writer.Write("let ");
                _writer.Write(ld.Local.Name);
                if (ld.Initializer != null)
                {
                    _writer.Write(" = ");
                    Print(ld.Initializer);
                }

                _writer.WriteLine();
                break;
            case BoundNodeKind.ReturnStatement:
                var rs = (BoundReturnStatement)node;
                _writer.Write("return ");
                if (rs.Expression != null)
                    Print(rs.Expression);

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
                var ce = (BoundCastExpression)node;
                _writer.Write("cast(");
                _writer.Write(ce.Type.Name);
                _writer.Write(", ");
                Print(ce.Expression);
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
                break;
            case BoundNodeKind.StructureLiteralExpression:
                break;
            case BoundNodeKind.UnaryExpression:
                var ue = (BoundUnaryExpression)node;
                _writer.Write(ue.Operator.Kind);
                _writer.Write('(');
                Print(ue.Operand);
                _writer.Write(')');
                break;
            case BoundNodeKind.NameExpression:
                _writer.Write(((BoundNameExpression)node).ReferencedSymbol.Name);
                break;
            case BoundNodeKind.BinaryExpression:
                var be = (BoundBinaryExpression)node;
                _writer.Write(be.Operator.Kind);
                _writer.Write('(');
                Print(be.Left);
                _writer.Write(',');
                _writer.Write(' ');
                Print(be.Right);
                _writer.Write(')');
                break;
            case BoundNodeKind.AssignmentExpression:
                var ae = (BoundAssignmentExpression)node;
                _writer.Write(ae.Assignee.Name);
                _writer.Write(" = ");
                Print(ae.Expression);
                break;
        }
    }
}
