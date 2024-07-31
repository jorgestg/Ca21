using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Diagnostics;
using Ca21.Binding;
using Ca21.Diagnostics;
using Ca21.Symbols;

namespace Ca21;

internal sealed class Compiler
{
    private readonly List<Diagnostic> _diagnostics = new();
    private readonly List<LocalSymbol> _locals = new(8);
    private readonly Stack<ControlBlockIdentifier> _controlBlocks = new(8);
    private readonly IndentedTextWriter _writer = new(new StringWriter());

    public ImmutableArray<Diagnostic> Diagnostics { get; private set; }

    public static Compiler Compile(SourceFunctionSymbol functionSymbol)
    {
        var compiler = new Compiler();
        var writer = compiler._writer;
        writer.WriteLine("(module");
        writer.Indent++;
        compiler.CompileFunction(functionSymbol);
        writer.Indent--;
        writer.Write(')');

        compiler.Diagnostics = compiler._diagnostics.ToImmutableArray();
        return compiler;
    }

    public override string ToString()
    {
        var stringWriter = (StringWriter)_writer.InnerWriter;
        return stringWriter.ToString();
    }

    private void CompileFunction(SourceFunctionSymbol functionSymbol)
    {
        var diagnostics = new DiagnosticList();
        var body = functionSymbol.Binder.BindBody(diagnostics);
        if (diagnostics.Any())
        {
            diagnostics.CopyTo(_diagnostics);
            return;
        }

        _writer.Write("(func");

        _writer.Write(" (export ");
        _writer.Write('"');
        _writer.Write(functionSymbol.Name);
        _writer.Write('"');
        _writer.Write(')');

        if (functionSymbol.ReturnType != TypeSymbol.Unit)
        {
            _writer.Write(" (result ");
            CompileType(functionSymbol.ReturnType);
            _writer.WriteLine(')');
        }

        CompileLocals(body);

        _writer.Indent++;
        CompileBlock(body);
        _writer.Indent--;
        _writer.WriteLine(')');
    }

    private void CompileLocals(BoundBlock body)
    {
        var localDeclarations = body.Statements.OfType<BoundLocalDeclaration>();
        if (!localDeclarations.Any())
            return;

        _writer.Write(" (local");
        foreach (var localDeclaration in localDeclarations)
        {
            _locals.Add(localDeclaration.Local);

            _writer.Write(' ');
            CompileType(localDeclaration.Local.Type);
        }

        _writer.WriteLine(')');
    }

    private void CompileType(TypeSymbol typeSymbol)
    {
        if (typeSymbol == TypeSymbol.Int32 || typeSymbol == TypeSymbol.Bool)
            _writer.Write("i32");
        else if (typeSymbol == TypeSymbol.Unit)
            return;
        else
            throw new UnreachableException();
    }

    private void CompileBlock(BoundBlock block)
    {
        foreach (var statement in block.Statements)
            CompileStatement(statement);
    }

    private void CompileStatement(BoundStatement statement)
    {
        switch (statement)
        {
            case BoundNopStatement:
                break;
            case BoundControlBlockStartStatement structureStart:
                CompileStructureStartStatement(structureStart);
                break;
            case BoundControlBlockEndStatement structureEnd:
                CompileStructureEndStatement(structureEnd);
                break;
            case BoundGotoStatement @goto:
                CompileGotoStatement(@goto);
                break;
            case BoundConditionalGotoStatement conditionalGoto:
                CompileConditionalGotoStatement(conditionalGoto);
                break;
            case BoundLocalDeclaration localDeclaration:
                CompileLocalDeclarationStatement(localDeclaration);
                break;
            case BoundReturnStatement returnStatement:
                CompileReturnStatement(returnStatement);
                break;
            case BoundBlock block:
                CompileBlock(block);
                break;
            case BoundExpressionStatement expressionStatement:
                CompileExpression(expressionStatement.Expression);
                break;
            default:
                throw new UnreachableException();
        }
    }

    private void CompileStructureStartStatement(BoundControlBlockStartStatement structureStart)
    {
        _controlBlocks.Push(structureStart.ControlBlockIdentifier);

        if (structureStart.IsLoop)
            _writer.WriteLine("loop");
        else
            _writer.WriteLine("block");

        _writer.Indent++;
    }

    private void CompileStructureEndStatement(BoundControlBlockEndStatement structureEnd)
    {
        _controlBlocks.Pop();

        _writer.Indent--;
        _writer.WriteLine("end");
    }

    private void CompileGotoStatement(BoundGotoStatement @goto)
    {
        _writer.Write("br ");
        _writer.WriteLine(GetControlBlockRelativeDepth(@goto.Target));
    }

    private int GetControlBlockRelativeDepth(ControlBlockIdentifier identifier)
    {
        var depth = 0;
        foreach (var structure in _controlBlocks)
        {
            if (structure == identifier)
                break;

            depth++;
        }

        return depth;
    }

    private void CompileConditionalGotoStatement(BoundConditionalGotoStatement conditionalGoto)
    {
        CompileExpression(conditionalGoto.Condition);
        if (conditionalGoto.BranchIfFalse)
            _writer.WriteLine("i32.eqz");

        _writer.Write("br_if ");
        _writer.WriteLine(GetControlBlockRelativeDepth(conditionalGoto.Target));
    }

    private void CompileLocalDeclarationStatement(BoundLocalDeclaration localDeclaration)
    {
        if (localDeclaration.Initializer == null)
            return;

        CompileExpression(localDeclaration.Initializer);
        _writer.Write("local.set ");
        CompileSymbolReference(localDeclaration.Local);
    }

    private void CompileReturnStatement(BoundReturnStatement returnStatement)
    {
        if (returnStatement.Value != null)
            CompileExpression(returnStatement.Value);

        _writer.WriteLine("return");
    }

    private void CompileExpression(BoundExpression expression)
    {
        switch (expression)
        {
            case BoundLiteralExpression literal:
                CompileLiteral(literal);
                break;
            case BoundNameExpression name:
                CompileNameExpression(name);
                break;
            case BoundBinaryExpression binaryExpression:
                CompileBinaryExpression(binaryExpression);
                break;
            case BoundAssignmentExpression assignment:
                CompileAssignmentExpression(assignment);
                break;
            default:
                throw new UnreachableException();
        }
    }

    private void CompileLiteral(BoundLiteralExpression literal)
    {
        CompileType(literal.Type);
        _writer.Write(".const ");
        _writer.Write(literal.Value);
        _writer.WriteLine();
    }

    private void CompileNameExpression(BoundNameExpression name)
    {
        _writer.Write("local.get ");
        CompileSymbolReference(name.ReferencedSymbol);
    }

    private void CompileSymbolReference(Symbol symbol)
    {
        switch (symbol)
        {
            case LocalSymbol local:
                var i = _locals.IndexOf(local);
                _writer.WriteLine(i);
                break;
            default:
                throw new UnreachableException();
        }
    }

    private void CompileBinaryExpression(BoundBinaryExpression binaryExpression)
    {
        CompileExpression(binaryExpression.Left);
        CompileExpression(binaryExpression.Right);
        CompileType(binaryExpression.Type);
        _writer.WriteLine(
            binaryExpression.Operator.Kind switch
            {
                BoundBinaryOperatorKind.Multiplication => ".mul",
                BoundBinaryOperatorKind.Division => ".div_s",
                BoundBinaryOperatorKind.Remainder => ".rem_s",
                BoundBinaryOperatorKind.Addition => ".add",
                BoundBinaryOperatorKind.Subtraction => ".sub",
                BoundBinaryOperatorKind.Greater => ".gt_s",
                BoundBinaryOperatorKind.GreaterOrEqual => ".ge_s",
                BoundBinaryOperatorKind.Less => ".lt_s",
                BoundBinaryOperatorKind.LessOrEqual => ".le_s",
                _ => throw new UnreachableException()
            }
        );
    }

    private void CompileAssignmentExpression(BoundAssignmentExpression assignment)
    {
        CompileExpression(assignment.Value);
        _writer.Write("local.set ");
        CompileSymbolReference(assignment.Assignee);
    }
}
