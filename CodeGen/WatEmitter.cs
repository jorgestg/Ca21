using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using Ca21.Binding;
using Ca21.Symbols;

namespace Ca21.CodeGen;

internal sealed class WatEmitter
{
    private readonly List<LocalSymbol> _locals = new(8);

    private readonly Dictionary<string, int> _stringLiterals = new(8);
    private int _lastOffset = 1;

    private readonly Stack<ControlBlockIdentifier> _controlBlocks = new(8);
    private readonly IndentedTextWriter _writer = new(new StringWriter());

    private WatEmitter(ModuleSymbol moduleSymbol)
    {
        ModuleSymbol = moduleSymbol;
    }

    public ModuleSymbol ModuleSymbol { get; }

    public static string Emit(ModuleSymbol moduleSymbol, ImmutableArray<BoundBlock> bodies)
    {
        Debug.Assert(moduleSymbol.Functions.Length == bodies.Length);

        var emitter = new WatEmitter(moduleSymbol);
        emitter.EmitModule(moduleSymbol, bodies);
        var stringWriter = (StringWriter)emitter._writer.InnerWriter;
        return stringWriter.ToString();
    }

    private void EmitModule(ModuleSymbol moduleSymbol, ImmutableArray<BoundBlock> bodies)
    {
        _writer.WriteLine("(module");
        _writer.Indent++;

        foreach (var (functionSymbol, body) in moduleSymbol.Functions.Zip(bodies))
            EmitFunction((SourceFunctionSymbol)functionSymbol, body);

        if (_stringLiterals.Count == 0)
        {
            _writer.Indent--;
            _writer.Write(')');
            return;
        }

        var pagesNeeded = Math.Ceiling(
            // Pages in WASM are 64 KB
            _stringLiterals.Sum(pair => Encoding.UTF8.GetByteCount(pair.Key)) / (64.0 * 1024.0)
        );

        _writer.Write("(memory (export \"memory\") ");
        _writer.Write(pagesNeeded);
        _writer.WriteLine(')');

        foreach (var (stringLiteral, offset) in _stringLiterals)
        {
            _writer.Write("(data (i32.const ");
            _writer.Write(offset);
            _writer.Write(')');
            _writer.Write(' ');
            _writer.Write(stringLiteral);
            _writer.WriteLine(')');
        }

        _writer.Indent--;
        _writer.Write(')');
    }

    private void EmitFunction(SourceFunctionSymbol functionSymbol, BoundBlock body)
    {
        _locals.Clear();

        _writer.Write("(func");

        _writer.Write(" (export ");
        _writer.Write('"');
        _writer.Write(functionSymbol.Name);
        _writer.Write('"');
        _writer.Write(')');

        if (functionSymbol.Parameters.Length > 0)
        {
            _writer.Write(" (param");
            foreach (var parameter in functionSymbol.Parameters)
            {
                _locals.Add(parameter);
                _writer.Write(' ');
                EmitType(parameter.Type);
            }

            _writer.Write(')');
        }

        if (functionSymbol.ReturnType != TypeSymbol.Unit)
        {
            _writer.Write(" (result ");
            EmitType(functionSymbol.ReturnType);
            _writer.WriteLine(')');
        }

        _writer.Indent++;
        
        var localDeclarations = body.Statements.OfType<BoundLocalDeclaration>();
        if (localDeclarations.Any())
        {
            _writer.Write("(local");
            foreach (var localDeclaration in localDeclarations)
            {
                _locals.Add(localDeclaration.Local);

                _writer.Write(' ');
                EmitType(localDeclaration.Local.Type);
            }

            _writer.WriteLine(')');
        }

        EmitBlock(body);
        _writer.Indent--;
        _writer.WriteLine(')');
    }

    private void EmitType(TypeSymbol typeSymbol)
    {
        if (typeSymbol == TypeSymbol.Int32 || typeSymbol == TypeSymbol.String || typeSymbol == TypeSymbol.Bool)
            _writer.Write("i32");
        else if (typeSymbol == TypeSymbol.Unit)
            return;
        else
            throw new UnreachableException();
    }

    private void EmitBlock(BoundBlock block)
    {
        foreach (var statement in block.Statements)
            EmitStatement(statement);
    }

    private void EmitStatement(BoundStatement statement)
    {
        switch (statement)
        {
            case BoundNopStatement:
                break;
            case BoundControlBlockStartStatement structureStart:
                EmitStructureStartStatement(structureStart);
                break;
            case BoundControlBlockEndStatement structureEnd:
                EmitStructureEndStatement(structureEnd);
                break;
            case BoundGotoStatement @goto:
                EmitGotoStatement(@goto);
                break;
            case BoundConditionalGotoStatement conditionalGoto:
                EmitConditionalGotoStatement(conditionalGoto);
                break;
            case BoundLocalDeclaration localDeclaration:
                EmitLocalDeclarationStatement(localDeclaration);
                break;
            case BoundReturnStatement returnStatement:
                EmitReturnStatement(returnStatement);
                break;
            case BoundBlock block:
                EmitBlock(block);
                break;
            case BoundExpressionStatement expressionStatement:
                EmitExpression(expressionStatement.Expression);
                break;
            default:
                throw new UnreachableException();
        }
    }

    private void EmitStructureStartStatement(BoundControlBlockStartStatement structureStart)
    {
        _controlBlocks.Push(structureStart.ControlBlockIdentifier);

        if (structureStart.IsLoop)
            _writer.WriteLine("loop");
        else
            _writer.WriteLine("block");

        _writer.Indent++;
    }

    private void EmitStructureEndStatement(BoundControlBlockEndStatement structureEnd)
    {
        _controlBlocks.Pop();

        _writer.Indent--;
        _writer.WriteLine("end");
    }

    private void EmitGotoStatement(BoundGotoStatement @goto)
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

    private void EmitConditionalGotoStatement(BoundConditionalGotoStatement conditionalGoto)
    {
        EmitExpression(conditionalGoto.Condition);
        if (conditionalGoto.BranchIfFalse)
            _writer.WriteLine("i32.eqz");

        _writer.Write("br_if ");
        _writer.WriteLine(GetControlBlockRelativeDepth(conditionalGoto.Target));
    }

    private void EmitLocalDeclarationStatement(BoundLocalDeclaration localDeclaration)
    {
        if (localDeclaration.Initializer == null)
            return;

        EmitExpression(localDeclaration.Initializer);
        _writer.Write("local.set ");
        EmitSymbolReference(localDeclaration.Local);
    }

    private void EmitReturnStatement(BoundReturnStatement returnStatement)
    {
        if (returnStatement.Value != null)
            EmitExpression(returnStatement.Value);

        _writer.WriteLine("return");
    }

    private void EmitExpression(BoundExpression expression)
    {
        switch (expression)
        {
            case BoundLiteralExpression literal:
                EmitLiteral(literal);
                break;
            case BoundNameExpression name:
                EmitNameExpression(name);
                break;
            case BoundCallExpression call:
                EmitCallExpression(call);
                break;
            case BoundBinaryExpression binaryExpression:
                EmitBinaryExpression(binaryExpression);
                break;
            case BoundAssignmentExpression assignment:
                EmitAssignmentExpression(assignment);
                break;
            default:
                throw new UnreachableException();
        }
    }

    private void EmitLiteral(BoundLiteralExpression literal)
    {
        if (literal.Type == TypeSymbol.String)
        {
            _writer.Write("i32.const ");

            var str = (string)literal.Value;
            if (_stringLiterals.TryGetValue(str, out var offset))
            {
                _writer.WriteLine(offset);
            }
            else
            {
                _stringLiterals.Add(str, _lastOffset);
                _writer.WriteLine(_lastOffset);
                _lastOffset += Encoding.UTF8.GetByteCount(str.AsSpan().Trim('"'));
            }

            _writer.WriteLine("i32.load");
        }
        else if (literal.Type == TypeSymbol.Int32 || literal.Type == TypeSymbol.Bool)
        {
            EmitType(literal.Type);
            _writer.Write(".const ");
            _writer.WriteLine(literal.Value);
        }
        else
        {
            throw new UnreachableException();
        }
    }

    private void EmitNameExpression(BoundNameExpression name)
    {
        _writer.Write("local.get ");
        EmitSymbolReference(name.ReferencedSymbol);
    }

    private void EmitSymbolReference(Symbol symbol)
    {
        var i = symbol switch
        {
            LocalSymbol local => _locals.IndexOf(local),
            FunctionSymbol function => ModuleSymbol.Functions.IndexOf(function),
            _ => throw new UnreachableException()
        };

        _writer.WriteLine(i);
    }

    private void EmitCallExpression(BoundCallExpression call)
    {
        foreach (var argument in call.Arguments)
            EmitExpression(argument);

        _writer.Write("call ");
        EmitSymbolReference(call.Function);
    }

    private void EmitBinaryExpression(BoundBinaryExpression binaryExpression)
    {
        EmitExpression(binaryExpression.Left);
        EmitExpression(binaryExpression.Right);
        EmitType(binaryExpression.Type);
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

    private void EmitAssignmentExpression(BoundAssignmentExpression assignment)
    {
        EmitExpression(assignment.Value);
        _writer.Write("local.set ");
        EmitSymbolReference(assignment.Assignee);
    }
}
