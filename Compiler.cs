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
    private readonly List<SourceLocalSymbol> _locals = new();
    private readonly StringWriter _forwardDeclarations = new();
    private readonly IndentedTextWriter _bodies = new(new StringWriter());

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
        _forwardDeclarations.WriteLine();
        _forwardDeclarations.WriteLine(_bodies.InnerWriter.ToString());
        return _forwardDeclarations.ToString();
    }

    private void AddDiagnosticsToBag(DiagnosticList diagnostics)
    {
        foreach (var diagnostic in diagnostics)
            _diagnostics.Add(diagnostic);
    }

    private void CompileFunction(SourceFunctionSymbol functionSymbol)
    {
        var diagnostics = new DiagnosticList();
        var body = functionSymbol.Binder.BindBody(diagnostics);
        body = Lowerer.Lower(body, lowerControlFlow: false);

        if (diagnostics.Count > 0)
        {
            AddDiagnosticsToBag(diagnostics);
            return;
        }

        CompileFunctionSignature(_forwardDeclarations, functionSymbol);
        _forwardDeclarations.WriteLine(';');

        CompileFunctionSignature(_bodies, functionSymbol);
        _bodies.Write(' ');
        _locals.Clear();
        CompileBlock(body);
    }

    private static void CompileFunctionSignature(TextWriter writer, SourceFunctionSymbol functionSymbol)
    {
        CompileType(writer, functionSymbol.ReturnType);
        writer.Write(' ');
        writer.Write(functionSymbol.Name);
        writer.Write('(');
        writer.Write(')');
    }

    private static void CompileType(TextWriter writer, TypeSymbol typeSymbol)
    {
        writer.Write(typeSymbol.Name);
    }

    private void CompileBlock(BoundBlock block)
    {
        _bodies.WriteLine('{');
        _bodies.Indent++;

        foreach (var statement in block.Statements)
            CompileStatement(statement);

        _bodies.Indent--;
        _bodies.WriteLine('}');
    }

    private void CompileExpression(BoundExpression expression)
    {
        switch (expression)
        {
            case BoundLiteral l:
                CompileLiteral(l);
                break;
            case BoundNameExpression n:
                CompileNameExpression(n);
                break;
            case BoundBinaryExpression b:
                CompileBinaryExpression(b);
                break;
            case BoundAssignmentExpression a:
                CompileAssignmentExpression(a);
                break;
            default:
                throw new UnreachableException();
        }
    }

    private void CompileLiteral(BoundLiteral literal)
    {
        _bodies.Write(literal.Value);
    }

    private void CompileNameExpression(BoundNameExpression name)
    {
        CompileSymbolReference(name.ReferencedSymbol);
    }

    private void CompileSymbolReference(Symbol symbol)
    {
        if (symbol is SourceLocalSymbol local)
        {
            _bodies.Write(local.Name);
            _bodies.Write('_');
            _bodies.Write(_locals.IndexOf(local));
        }
        else
        {
            _bodies.Write(symbol.Name);
        }
    }

    private void CompileBinaryExpression(BoundBinaryExpression binaryExpression)
    {
        CompileExpression(binaryExpression.Left);
        _bodies.Write(' ');
        _bodies.Write(
            binaryExpression.Operator.Kind switch
            {
                BoundBinaryOperatorKind.Multiplication => "*",
                BoundBinaryOperatorKind.Division => "/",
                BoundBinaryOperatorKind.Remainder => "%",
                BoundBinaryOperatorKind.Addition => "+",
                BoundBinaryOperatorKind.Subtraction => "-",
                BoundBinaryOperatorKind.GreaterThan => ">",
                BoundBinaryOperatorKind.GreaterThanOrEqual => ">=",
                BoundBinaryOperatorKind.LessThan => "<",
                BoundBinaryOperatorKind.LessThanOrEqual => "<=",
                _ => throw new UnreachableException()
            }
        );

        _bodies.Write(' ');
        CompileExpression(binaryExpression.Right);
    }

    private void CompileAssignmentExpression(BoundAssignmentExpression assignment)
    {
        CompileSymbolReference(assignment.Assignee);
        _bodies.Write(' ');
        _bodies.Write('=');
        _bodies.Write(' ');
        CompileExpression(assignment.Value);
    }

    private void CompileStatement(BoundStatement statement)
    {
        switch (statement)
        {
            case BoundLocalDeclaration d:
                CompileLocalDeclaration(d);
                break;
            case BoundWhileStatement w:
                CompileWhileLoop(w);
                break;
            case BoundReturnStatement r:
                CompileReturnStatement(r);
                break;
            case BoundBlock b:
                CompileBlock(b);
                break;
            case BoundExpressionStatement e:
                CompileExpression(e.Expression);
                _bodies.WriteLine(';');
                break;
            default:
                throw new UnreachableException();
        }
    }

    private void CompileLocalDeclaration(BoundLocalDeclaration declaration)
    {
        _locals.Add(declaration.Local);

        if (!declaration.Local.IsMutable)
        {
            _bodies.Write("const");
            _bodies.Write(' ');
        }

        CompileType(_bodies, declaration.Local.Type);
        _bodies.Write(' ');
        _bodies.Write(declaration.Local.Name);
        _bodies.Write('_');
        _bodies.Write(_locals.Count - 1);
        _bodies.Write(' ');
        _bodies.Write('=');
        _bodies.Write(' ');
        CompileExpression(declaration.Initializer);
        _bodies.WriteLine(';');
    }

    private void CompileWhileLoop(BoundWhileStatement whileLoop)
    {
        _bodies.Write("while");
        _bodies.Write(' ');
        _bodies.Write('(');
        CompileExpression(whileLoop.Condition);
        _bodies.Write(')');
        _bodies.Write(' ');
        CompileBlock(whileLoop.Body);
    }

    private void CompileReturnStatement(BoundReturnStatement returnStatement)
    {
        _bodies.Write("return");
        if (returnStatement.Value != null)
        {
            _bodies.Write(' ');
            CompileExpression(returnStatement.Value);
        }

        _bodies.WriteLine(';');
    }
}
