using System.CodeDom.Compiler;
using System.Collections.Frozen;
using System.Diagnostics;
using System.Text;
using Ca21.Binding;
using Ca21.Symbols;

namespace Ca21.CodeGen;

internal sealed class WatEmitter
{
    private readonly IndentedTextWriter _writer;

    private readonly Dictionary<string, int> _stringLiterals = new(8);
    private int _lastLiteralOffset = 1;
    private int _minimumStackSize;

    private int _functionStackSize;
    private readonly List<LocalSymbol> _locals = new(8);
    private readonly Stack<ControlBlockIdentifier> _controlBlocks = new(8);

    private WatEmitter(
        ModuleSymbol moduleSymbol,
        FrozenDictionary<FunctionSymbol, BoundBlock> bodies,
        TextWriter writer
    )
    {
        ModuleSymbol = moduleSymbol;
        Bodies = bodies;

        _writer = new IndentedTextWriter(writer);
    }

    public ModuleSymbol ModuleSymbol { get; }
    public FrozenDictionary<FunctionSymbol, BoundBlock> Bodies { get; }

    public static void Emit(
        ModuleSymbol moduleSymbol,
        FrozenDictionary<FunctionSymbol, BoundBlock> bodies,
        TextWriter writer
    )
    {
        var emitter = new WatEmitter(moduleSymbol, bodies, writer);
        emitter.EmitModule();
    }

    private void EmitModule()
    {
        _writer.WriteLine("(module");
        _writer.Indent++;

        foreach (var functionSymbol in ModuleSymbol.Functions)
            EmitFunction(functionSymbol);

        const int pageSizeInBytes = 64 * 1024;
        var pages = 0;

        // Data section
        if (_stringLiterals.Count > 0)
        {
            Span<byte> lengthBytes = stackalloc byte[4];
            foreach (var (stringLiteral, offset) in _stringLiterals)
            {
                var unquotedLiteral = stringLiteral.AsSpan().Trim('"');
                _writer.Write("(data (i32.const ");
                _writer.Write(offset);
                _writer.Write(')');
                _writer.Write(' ');
                _writer.Write('"');

                // Append the int32 length at the beginning of the string
                var length = (uint)Encoding.UTF8.GetByteCount(unquotedLiteral);
                BitConverter.TryWriteBytes(lengthBytes, length);
                foreach (var @byte in lengthBytes)
                {
                    _writer.Write('\\');
                    _writer.Write(@byte.ToString("X2"));
                }

                _writer.Write(unquotedLiteral);
                _writer.Write('"');
                _writer.WriteLine(')');
            }

            _writer.Write("(global $data_end i32 (i32.const ");
            _writer.Write(_lastLiteralOffset);
            _writer.Write(')');
            _writer.WriteLine(')');

            pages += (int)Math.Ceiling((double)_lastLiteralOffset / pageSizeInBytes);
        }

        if (_minimumStackSize > 0)
        {
            pages += (int)Math.Ceiling((double)_minimumStackSize / pageSizeInBytes);

            _writer.Write("(global $stack_pointer i32 (i32.const ");
            _writer.Write(pages * pageSizeInBytes - 1);
            _writer.Write(')');
            _writer.WriteLine(')');
        }

        if (pages > 0)
        {
            _writer.Write("(memory (export \"memory\") ");
            _writer.Write(pages);
            _writer.WriteLine(')');
        }

        _writer.Indent--;
        _writer.Write(')');
    }

    private void EmitFunction(FunctionSymbol functionSymbol)
    {
        var sourceFunctionSymbol = (SourceFunctionSymbol)functionSymbol;
        if (sourceFunctionSymbol.IsExtern)
        {
            EmitExternFunction(sourceFunctionSymbol);
            return;
        }

        EmitTopLevelFunction(sourceFunctionSymbol, Bodies[functionSymbol]);
    }

    private void EmitExternFunction(SourceFunctionSymbol functionSymbol)
    {
        Debug.Assert(functionSymbol.IsExtern);

        _writer.Write("(import ");

        var nameSpan = functionSymbol.ExternName.AsSpan();
        var namespaceSeparator = nameSpan.IndexOf(" ");
        var @namespace = nameSpan[0..namespaceSeparator];
        _writer.Write(@namespace);
        _writer.Write('"');
        _writer.Write(' ');

        var functionName = nameSpan[(namespaceSeparator + 1)..];
        _writer.Write('"');
        _writer.Write(functionName);
        _writer.Write(' ');

        EmitFunctionSignature(functionSymbol);
        _writer.Write(')');
        _writer.WriteLine(')');
    }

    private void EmitTopLevelFunction(SourceFunctionSymbol functionSymbol, BoundBlock body)
    {
        EmitFunctionSignature(functionSymbol);
        _writer.WriteLine();
        _writer.Indent++;

        // Add stack pointer local
        if (AllocatesOnTheStack(body))
            _locals.Add(new SyntheticLocalSymbol(TypeSymbol.Int32));

        foreach (var statement in body.Statements)
        {
            if (statement is BoundLocalDeclaration localDeclaration)
                _locals.Add(localDeclaration.Local);
        }

        if (_locals.Count > 0)
        {
            _writer.Write("(local");
            foreach (var local in _locals)
            {
                _writer.Write(' ');
                EmitType(local.Type);
            }

            _writer.WriteLine(')');
        }

        EmitBlock(body);
        _writer.Indent--;
        _writer.WriteLine(')');

        _minimumStackSize += _functionStackSize;
        _locals.Clear();
    }

    private static bool AllocatesOnTheStack(BoundBlock body)
    {
        foreach (var statement in body.Statements)
        {
            switch (statement)
            {
                case BoundNopStatement:
                case BoundControlBlockStartStatement:
                case BoundControlBlockEndStatement:
                case BoundGotoStatement:
                    break;
                case BoundConditionalGotoStatement conditionalGoto:
                    if (IsStackAllocation(conditionalGoto.Condition))
                        return true;

                    break;
                case BoundLocalDeclaration localDeclaration:
                    if (localDeclaration.Initializer != null && IsStackAllocation(localDeclaration.Initializer))
                        return true;

                    break;
                case BoundReturnStatement @return:
                    if (@return.Value != null && IsStackAllocation(@return.Value))
                        return true;

                    break;
                case BoundExpressionStatement expressionStatement:
                    if (IsStackAllocation(expressionStatement.Expression))
                        return true;

                    break;
                default:
                    throw new UnreachableException();
            }
        }

        return false;

        static bool IsStackAllocation(BoundExpression expression)
        {
            return expression switch
            {
                BoundStructureLiteralExpression structureLiteral => true,
                BoundLiteralExpression or BoundNameExpression => false,
                BoundCallExpression call => call.Arguments.Any(IsStackAllocation),
                BoundBinaryExpression binaryExpression
                    => IsStackAllocation(binaryExpression.Left) || IsStackAllocation(binaryExpression.Right),
                BoundAssignmentExpression assignment => IsStackAllocation(assignment.Value),
                _ => throw new UnreachableException(),
            };
        }
    }

    private void EmitFunctionSignature(SourceFunctionSymbol functionSymbol)
    {
        _writer.Write("(func");

        if (functionSymbol.IsExported)
        {
            _writer.Write(" (export \"");
            _writer.Write(functionSymbol.Name);
            _writer.Write('"');
            _writer.Write(')');
        }

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
            _writer.Write(')');
        }
    }

    private void EmitType(TypeSymbol typeSymbol)
    {
        if (typeSymbol.NativeType != NativeType.Unit)
        {
            _writer.Write("i32");
            return;
        }
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

    private void EmitStructureEndStatement(BoundControlBlockEndStatement _)
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
            case BoundStructureLiteralExpression structureLiteral:
                EmitStructureLiteralExpression(structureLiteral);
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
                _stringLiterals.Add(str, _lastLiteralOffset);
                _writer.WriteLine(_lastLiteralOffset);
                _lastLiteralOffset += Encoding.UTF8.GetByteCount(str.AsSpan().Trim('"'));
            }
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
            FunctionSymbol function => ModuleSymbol.Functions.IndexOf((SourceFunctionSymbol)function),
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

    private void EmitStructureLiteralExpression(BoundStructureLiteralExpression structureLiteral)
    {
        _functionStackSize += GetTypeSize(structureLiteral.Type);

        _writer.WriteLine("global.get $stack_pointer");
        _writer.WriteLine("local.tee 0");

        var initializers = structureLiteral.FieldInitializers;
        var offset = 0;
        for (var i = initializers.Length - 1; i >= 0; i--)
        {
            var initializer = initializers[i];
            offset += GetTypeSize(initializer.Field.Type);

            _writer.Write("i32.const ");
            _writer.WriteLine(offset);
            _writer.WriteLine("i32.sub");
            _writer.WriteLine("local.tee 0");
            EmitExpression(initializer.Value);
            _writer.WriteLine("i32.store");
            _writer.WriteLine("local.get 0");
        }

        _writer.WriteLine("global.set $stack_pointer");
        _writer.WriteLine("local.get 0");
    }

    private static int GetTypeSize(TypeSymbol typeSymbol)
    {
        return typeSymbol.NativeType switch
        {
            NativeType.Unit => 0,
            NativeType.Bool => 1,
            NativeType.Int32 => 4,
            NativeType.String => 8,
            NativeType.None => GetStructSize((StructureSymbol)typeSymbol),
            _ => throw new UnreachableException()
        };

        static int GetStructSize(StructureSymbol structureSymbol)
        {
            var size = 0;
            foreach (var field in structureSymbol.Fields)
                size += GetTypeSize(field.Type);

            return size;
        }
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
