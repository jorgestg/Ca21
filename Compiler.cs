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
    private readonly List<LocalSymbol> _locals = new();
    private readonly IndentedTextWriter _writer = new(new StringWriter());

    public ImmutableArray<Diagnostic> Diagnostics { get; private set; }

    public static Compiler Compile(SourceFunctionSymbol functionSymbol)
    {
        var compiler = new Compiler();
        compiler.CompileFunction(functionSymbol);
        compiler.Diagnostics = compiler._diagnostics.ToImmutableArray();
        return compiler;
    }

    public override string ToString()
    {
        var stringWriter = (StringWriter)_writer.InnerWriter;
        return stringWriter.ToString();
    }

    private void AddDiagnosticsToBag(DiagnosticList diagnostics)
    {
        foreach (var diagnostic in diagnostics)
            _diagnostics.Add(diagnostic);
    }

    private void CompileName(string name)
    {
        _writer.Write('$');
        _writer.Write(name);
    }

    private void CompileFunction(SourceFunctionSymbol functionSymbol)
    {
        var diagnostics = new DiagnosticList();
        var body = functionSymbol.Binder.BindBody(diagnostics);
        if (diagnostics.Count > 0)
        {
            AddDiagnosticsToBag(diagnostics);
            return;
        }

        _writer.Write("export");
        _writer.Write(' ');
        _writer.Write("function");
        _writer.Write(' ');
        CompileType(functionSymbol.ReturnType);
        _writer.Write(' ');
        CompileName(functionSymbol.Name);
        _writer.Write('(');
        _writer.Write(')');
        _writer.Write(' ');
        _locals.Clear();
        CompileBlock(body);
        _locals.Clear();
    }

    private void CompileType(TypeSymbol typeSymbol)
    {
        if (typeSymbol == TypeSymbol.Int32)
            _writer.Write('w');

        if (typeSymbol == TypeSymbol.Bool)
            _writer.Write('b');

        if (typeSymbol == TypeSymbol.Unit)
            return;
    }

    private void CompileBlock(BoundBlock block)
    {
        _writer.WriteLine('{');
        _writer.WriteLine("@start");
        _writer.Indent++;

        foreach (var statement in block.Statements)
            CompileStatement(statement);

        _writer.Indent--;
        _writer.WriteLine('}');
    }

    private void CompileStatement(BoundStatement statement)
    {
        switch (statement)
        {
            case BoundNopStatement:
                break;
            case BoundLabelDeclarationStatement labelDeclaration:
                CompileLabelStatement(labelDeclaration);
                break;
            case BoundGotoStatement @goto:
                CompileGotoStatement(@goto);
                break;
            case BoundConditionalGotoStatement conditionalGoto:
                CompileConditionalGotoStatement(conditionalGoto);
                break;
            case BoundLocalDeclaration localDeclaration:
                CompileLocalDeclaration(localDeclaration);
                break;
            case BoundReturnStatement returnStatement:
                CompileReturnStatement(returnStatement);
                break;
            case BoundBlock block:
                CompileBlock(block);
                break;
            case BoundExpressionStatement expressionStatement:
                CompileExpression(expressionStatement.Expression);
                _writer.WriteLine();
                break;
            default:
                throw new UnreachableException();
        }
    }

    private void CompileLabelStatement(BoundLabelDeclarationStatement declaration)
    {
        if (!_locals.Contains(declaration.Label))
            _locals.Add(declaration.Label);

        _writer.Indent--;
        CompileSymbolReference(declaration.Label);
        _writer.WriteLine();
        _writer.Indent++;
    }

    private void CompileGotoStatement(BoundGotoStatement @goto)
    {
        if (!_locals.Contains(@goto.Target))
            _locals.Add(@goto.Target);

        _writer.Write("jmp");
        _writer.Write(' ');
        CompileSymbolReference(@goto.Target);
        _writer.WriteLine();
    }

    private void CompileConditionalGotoStatement(BoundConditionalGotoStatement conditionalGoto)
    {
        if (!_locals.Contains(conditionalGoto.Then))
            _locals.Add(conditionalGoto.Then);

        if (!_locals.Contains(conditionalGoto.Otherwise))
            _locals.Add(conditionalGoto.Otherwise);

        _writer.Write("jnz");
        _writer.Write(' ');
        CompileExpression(conditionalGoto.Condition);
        _writer.Write(' ');
        CompileSymbolReference(conditionalGoto.Then);
        _writer.Write(' ');
        CompileSymbolReference(conditionalGoto.Otherwise);
        _writer.WriteLine();
    }

    private void CompileLocalDeclaration(BoundLocalDeclaration declaration)
    {
        _locals.Add(declaration.Local);

        CompileSymbolReference(declaration.Local);
        _writer.Write(' ');
        _writer.Write('=');
        CompileType(declaration.Local.Type);
        _writer.Write(' ');
        CompileExpression(declaration.Initializer);
        _writer.WriteLine();
    }

    private void CompileReturnStatement(BoundReturnStatement returnStatement)
    {
        _writer.Write("ret");
        if (returnStatement.Value != null)
        {
            _writer.Write(' ');
            CompileExpression(returnStatement.Value);
        }

        _writer.WriteLine();
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
        _writer.Write(literal.Value);
    }

    private void CompileNameExpression(BoundNameExpression name)
    {
        CompileSymbolReference(name.ReferencedSymbol);
    }

    private void CompileSymbolReference(Symbol symbol)
    {
        switch (symbol)
        {
            case LabelSymbol label:
                _writer.Write('@');
                _writer.Write(symbol.Name);
                _writer.Write('_');
                _writer.Write(_locals.IndexOf(label));
                break;
            case LocalSymbol local:
                _writer.Write('%');
                _writer.Write(symbol.Name);
                _writer.Write('_');
                _writer.Write(_locals.IndexOf(local));
                break;
            default:
                _writer.Write('$');
                _writer.Write(symbol.Name);
                break;
        }
    }

    private void CompileBinaryExpression(BoundBinaryExpression binaryExpression)
    {
        _writer.Write(
            binaryExpression.Operator.Kind switch
            {
                BoundBinaryOperatorKind.Multiplication => "mul",
                BoundBinaryOperatorKind.Division => "div",
                BoundBinaryOperatorKind.Remainder => "rem",
                BoundBinaryOperatorKind.Addition => "add",
                BoundBinaryOperatorKind.Subtraction => "sub",
                BoundBinaryOperatorKind.Greater => "sgt",
                BoundBinaryOperatorKind.GreaterOrEqual => "sge",
                BoundBinaryOperatorKind.Less => "slt",
                BoundBinaryOperatorKind.LessOrEqual => "sle",
                _ => throw new UnreachableException()
            }
        );

        _writer.Write(' ');
        CompileExpression(binaryExpression.Left);
        _writer.Write(' ');
        CompileExpression(binaryExpression.Right);
    }

    private void CompileAssignmentExpression(BoundAssignmentExpression assignment)
    {
        CompileSymbolReference(assignment.Assignee);
        _writer.Write(' ');
        _writer.Write('=');
        _writer.Write(' ');
        CompileExpression(assignment.Value);
    }
}
