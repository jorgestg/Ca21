using System.CodeDom.Compiler;
using System.Diagnostics;
using Ca21.Binding;
using Ca21.Symbols;

namespace Ca21.Backends;

internal sealed class C99Backend
{
    private readonly IndentedTextWriter _output;
    private readonly Dictionary<Symbol, string> _mangledNames = new();
    private readonly List<LocalSymbol> _locals = new(25);
    private readonly Dictionary<string, string> _constants;

    private C99Backend(Compiler compiler, TextWriter writer)
    {
        _output = new IndentedTextWriter(writer);
        _constants = new Dictionary<string, string>(compiler.Constants.Count);

        Compiler = compiler;
        Writer = writer;
    }

    public Compiler Compiler { get; }
    public TextWriter Writer { get; }

    public static C99Backend Emit(Compiler compiler, TextWriter writer)
    {
        var backend = new C99Backend(compiler, writer);
        backend.Emit();
        return backend;
    }

    private void Emit()
    {
        _output.WriteLine("#include <stdint.h>");
        _output.WriteLine("#include <stdbool.h>");
        _output.WriteLine();

        Span<byte> lengthBytes = stackalloc byte[4];
        Span<char> lengthChars = stackalloc char[2];
        foreach (var constant in Compiler.Constants)
        {
            var name = MangleName("CS");
            _constants[constant] = name;

            _output.Write("static const char ");
            _output.Write(name);
            _output.Write('[');
            _output.Write(constant.Length + 4);
            _output.Write("] = \"");

            BitConverter.TryWriteBytes(lengthBytes, constant.Length);
            foreach (var @byte in lengthBytes)
            {
                _output.Write("\\x");
                @byte.TryFormat(lengthChars, out _, "X2");
                _output.Write(lengthChars);
            }

            _output.Write(constant);
            _output.Write('"');
            _output.WriteLine(';');
        }

        EmitModule(Compiler.ModuleSymbol);
    }

    private void EmitModule(ModuleSymbol moduleSymbol)
    {
        // C doesn't support forward-references, so we have to emit in order
        var structures = moduleSymbol.GetMembers<StructureSymbol>();
        var emitted = new HashSet<IModuleMemberSymbol>(structures.Count());
        foreach (var structure in structures)
        {
            foreach (var field in structure.Fields)
            {
                if (field.Type.TypeKind != TypeKind.None)
                    continue;

                var fieldTypeAsStructure = (StructureSymbol)field.Type;
                if (emitted.Contains(fieldTypeAsStructure))
                    continue;

                EmitStructure(fieldTypeAsStructure);
                emitted.Add(fieldTypeAsStructure);
            }

            EmitStructure(structure);
            _output.WriteLine();
        }

        var functions = moduleSymbol.GetMembers<SourceFunctionSymbol>();
        foreach (var functionSymbol in functions)
            EmitFunctionSignature(functionSymbol, emitSemicolon: true);

        _output.WriteLine();

        foreach (var functionSymbol in functions)
        {
            if (functionSymbol.IsExtern)
                continue;

            EmitFunction(functionSymbol);
            _output.WriteLine();
        }
    }

    private void EmitMangledName(Symbol symbol)
    {
        switch (symbol.SymbolKind)
        {
            case SymbolKind.Field:
            {
                var field = (FieldSymbol)symbol;
                _output.Write(field.Name);
                break;
            }

            case SymbolKind.Local:
            {
                var local = (LocalSymbol)symbol;
                _output.Write(local.Name);
                _output.Write('_');
                _output.Write(_locals.IndexOf(local));
                break;
            }

            case SymbolKind.Function:
            {
                var function = (SourceFunctionSymbol)symbol;
                if (function.IsExtern || function.IsExported)
                {
                    _output.Write(function.ExternName ?? function.Name);
                    break;
                }

                EmitMangledNameCore(symbol);
                break;
            }

            case SymbolKind.Type:
                EmitMangledNameCore(symbol);
                break;

            default:
                throw new UnreachableException();
        }

        return;

        void EmitMangledNameCore(Symbol symbol)
        {
            if (_mangledNames.TryGetValue(symbol, out var name))
            {
                _output.Write(name);
                return;
            }

            name = MangleName(symbol.Name);
            _mangledNames.Add(symbol, name);
            _output.Write(name);
        }
    }

    private static string MangleName(string name)
    {
        return string.Create(
            name.Length + 11,
            name,
            (buffer, symbol) =>
            {
                name.AsSpan().CopyTo(buffer);
                buffer[name.Length] = '_';

                var randomNumber = Random.Shared.Next(0x0, 0xFF);
                buffer = buffer.Slice(name.Length + 1);
                randomNumber.TryFormat(buffer, out _, "X2");

                var hashCode = symbol.GetHashCode();
                buffer = buffer.Slice(2);
                hashCode.TryFormat(buffer, out _, "X8");
            }
        );
    }

    private void EmitStructure(StructureSymbol structureSymbol)
    {
        _output.Write("struct ");
        EmitMangledName(structureSymbol);
        _output.WriteLine();
        _output.WriteLine('{');
        _output.Indent++;

        foreach (var field in structureSymbol.Fields)
        {
            EmitTypeReference(field.Type);
            _output.Write(' ');
            _output.Write(field.Name);
            _output.WriteLine(';');
        }

        _output.Indent--;
        _output.WriteLine("};");
    }

    private void EmitTypeReference(TypeSymbol typeSymbol)
    {
        switch (typeSymbol.TypeKind)
        {
            case TypeKind.Structure:
            case TypeKind.Enumeration:
                _output.Write("struct ");
                EmitMangledName(typeSymbol);
                break;
            case TypeKind.Void:
                _output.Write("void");
                break;
            case TypeKind.Int32:
                _output.Write("int32_t");
                break;
            case TypeKind.Int64:
                _output.Write("int64_t");
                break;
            case TypeKind.Bool:
                _output.Write("bool");
                break;
            case TypeKind.String:
                _output.Write("char*");
                break;
            default:
                throw new UnreachableException();
        }
    }

    private void EmitFunction(SourceFunctionSymbol functionSymbol)
    {
        _locals.Clear();

        foreach (var parameter in functionSymbol.Parameters)
            _locals.Add(parameter);

        EmitFunctionSignature(functionSymbol, emitSemicolon: false);

        _output.WriteLine();
        _output.WriteLine('{');
        _output.Indent++;

        var cfg = Compiler.Bodies[functionSymbol];
        foreach (var statement in cfg.Statements)
        {
            switch (statement.Kind)
            {
                case BoundNodeKind.LabelStatement:
                    _locals.Add(((BoundLabelStatement)statement).Label);
                    break;
                case BoundNodeKind.GotoStatement:
                    _locals.Add(((BoundGotoStatement)statement).Target);
                    break;
                case BoundNodeKind.ConditionalGotoStatement:
                    _locals.Add(((BoundConditionalGotoStatement)statement).Target);
                    break;
                case BoundNodeKind.LocalDeclaration:
                    _locals.Add(((BoundLocalDeclaration)statement).Local);
                    break;
            }

            EmitStatement(statement);
        }

        _output.Indent--;
        _output.WriteLine('}');
    }

    private void EmitFunctionSignature(SourceFunctionSymbol functionSymbol, bool emitSemicolon)
    {
        if (!functionSymbol.IsExtern && !functionSymbol.IsExported)
            _output.Write("static ");

        EmitTypeReference(functionSymbol.ReturnType);
        _output.Write(' ');
        EmitMangledName(functionSymbol);
        _output.Write('(');
        var isFirst = true;
        for (int i = 0; i < functionSymbol.Parameters.Length; i++)
        {
            var parameter = functionSymbol.Parameters[i];
            if (!isFirst)
                _output.Write(", ");

            if (!parameter.IsMutable)
                _output.Write("const ");

            EmitTypeReference(parameter.Type);
            _output.Write(' ');
            _output.Write(parameter.Name);
            _output.Write('_');
            _output.Write(i);

            isFirst = false;
        }

        _output.Write(')');

        if (emitSemicolon)
            _output.WriteLine(';');
    }

    private void EmitStatement(BoundStatement statement)
    {
        switch (statement.Kind)
        {
            case BoundNodeKind.ExpressionStatement:
                EmitExpressionStatement((BoundExpressionStatement)statement);
                break;
            case BoundNodeKind.NopStatement:
                break;
            case BoundNodeKind.LabelStatement:
                EmitLabelStatement((BoundLabelStatement)statement);
                break;
            case BoundNodeKind.GotoStatement:
                EmitGotoStatement((BoundGotoStatement)statement);
                break;
            case BoundNodeKind.ConditionalGotoStatement:
                EmitConditionalGotoStatement((BoundConditionalGotoStatement)statement);
                break;
            case BoundNodeKind.LocalDeclaration:
                EmitLocalDeclaration((BoundLocalDeclaration)statement);
                break;
            case BoundNodeKind.ReturnStatement:
                EmitReturnStatement((BoundReturnStatement)statement);
                break;
            default:
                throw new UnreachableException();
        }
    }

    private void EmitExpressionStatement(BoundExpressionStatement statement)
    {
        EmitExpression(statement.Expression);
        _output.WriteLine(';');
    }

    private void EmitLabelStatement(BoundLabelStatement statement)
    {
        _output.Indent--;
        EmitMangledName(statement.Label);
        _output.WriteLine(':');
        _output.Indent++;
    }

    private void EmitGotoStatement(BoundGotoStatement statement)
    {
        _output.Write("goto ");
        EmitMangledName(statement.Target);
        _output.WriteLine(';');
    }

    private void EmitConditionalGotoStatement(BoundConditionalGotoStatement statement)
    {
        _output.Write("if (");
        if (statement.BranchIfFalse)
            _output.Write("!(");

        EmitExpression(statement.Condition);
        if (statement.BranchIfFalse)
            _output.Write(')');

        _output.WriteLine(')');
        _output.Indent++;
        _output.Write("goto ");
        EmitMangledName(statement.Target);
        _output.WriteLine(';');
        _output.Indent--;
    }

    private void EmitLocalDeclaration(BoundLocalDeclaration statement)
    {
        _locals.Add(statement.Local);

        EmitTypeReference(statement.Local.Type);
        _output.Write(' ');
        EmitMangledName(statement.Local);
        if (statement.Initializer != null)
        {
            _output.Write(" = ");
            EmitExpression(statement.Initializer);
        }

        _output.WriteLine(';');
    }

    private void EmitReturnStatement(BoundReturnStatement statement)
    {
        _output.Write("return");
        if (statement.Value != null)
        {
            _output.Write(' ');
            EmitExpression(statement.Value);
        }

        _output.WriteLine(';');
    }

    private void EmitExpression(BoundExpression expression)
    {
        switch (expression.Kind)
        {
            case BoundNodeKind.CastExpression:
                EmitCastExpression((BoundCastExpression)expression);
                break;
            case BoundNodeKind.LiteralExpression:
                EmitLiteralExpression((BoundLiteralExpression)expression);
                break;
            case BoundNodeKind.CallExpression:
                EmitCallExpression((BoundCallExpression)expression);
                break;
            case BoundNodeKind.AccessExpression:
                EmitAccessExpression((BoundAccessExpression)expression);
                break;
            case BoundNodeKind.StructureLiteralExpression:
                EmitStructureLiteralExpression((BoundStructureLiteralExpression)expression);
                break;
            case BoundNodeKind.UnaryExpression:
                EmitUnaryExpression((BoundUnaryExpression)expression);
                break;
            case BoundNodeKind.NameExpression:
                EmitNameExpression((BoundNameExpression)expression);
                break;
            case BoundNodeKind.BinaryExpression:
                EmitBinaryExpression((BoundBinaryExpression)expression);
                break;
            case BoundNodeKind.AssignmentExpression:
                EmitAssignmentExpression((BoundAssignmentExpression)expression);
                break;
            default:
                throw new UnreachableException();
        }
    }

    private void EmitCastExpression(BoundCastExpression expression)
    {
        _output.Write('(');
        _output.Write('(');
        EmitTypeReference(expression.Type);
        _output.Write(')');
        EmitExpression(expression.Expression);
        _output.Write(')');
    }

    private void EmitLiteralExpression(BoundLiteralExpression expression)
    {
        Debug.Assert(expression.Value != null);

        if (expression.Value is not string str)
        {
            _output.Write(expression.Value.ToString());
            return;
        }

        _output.Write(_constants[str]);
    }

    private void EmitCallExpression(BoundCallExpression expression)
    {
        var function = (SourceFunctionSymbol)expression.Function;
        EmitMangledName(function);
        _output.Write('(');
        var isFirst = true;
        foreach (var argument in expression.Arguments)
        {
            if (!isFirst)
                _output.Write(", ");

            EmitExpression(argument);
            isFirst = false;
        }

        _output.Write(')');
    }

    private void EmitAccessExpression(BoundAccessExpression expression)
    {
        EmitExpression(expression.Left);
        _output.Write('.');
        _output.Write(expression.ReferencedMember.Name);
    }

    private void EmitStructureLiteralExpression(BoundStructureLiteralExpression expression)
    {
        _output.Write(expression.Structure.Name);
    }

    private void EmitUnaryExpression(BoundUnaryExpression expression)
    {
        _output.Write(
            expression.Operator.Kind switch
            {
                BoundOperatorKind.LogicalNot => '!',
                BoundOperatorKind.Negation => '-',
                _ => throw new UnreachableException()
            }
        );

        EmitExpression(expression.Operand);
    }

    private void EmitNameExpression(BoundNameExpression expression) => EmitMangledName(expression.ReferencedSymbol);

    private void EmitBinaryExpression(BoundBinaryExpression expression)
    {
        EmitExpression(expression.Left);
        _output.Write(' ');
        _output.Write(
            expression.Operator.Kind switch
            {
                BoundOperatorKind.Addition => "+",
                BoundOperatorKind.Subtraction => "-",
                BoundOperatorKind.Multiplication => "*",
                BoundOperatorKind.Division => "/",
                BoundOperatorKind.Remainder => "%",
                BoundOperatorKind.Equality => "==",
                BoundOperatorKind.Inequality => "!=",
                BoundOperatorKind.Less => "<",
                BoundOperatorKind.LessOrEqual => "<=",
                BoundOperatorKind.Greater => ">",
                BoundOperatorKind.GreaterOrEqual => ">=",
                _ => throw new UnreachableException()
            }
        );

        _output.Write(' ');
        EmitExpression(expression.Right);
    }

    private void EmitAssignmentExpression(BoundAssignmentExpression expression)
    {
        EmitMangledName(expression.Assignee);
        _output.Write(" = ");
        EmitExpression(expression.Value);
    }
}
