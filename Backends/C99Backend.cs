using System.CodeDom.Compiler;
using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using Ca21.Binding;
using Ca21.Symbols;

namespace Ca21.Backends;

internal sealed class C99Backend
{
    private readonly IndentedTextWriter _output;

    private C99Backend(
        ModuleSymbol moduleSymbol,
        FrozenDictionary<SourceFunctionSymbol, ControlFlowGraph> bodies,
        TextWriter writer
    )
    {
        _output = new IndentedTextWriter(writer);

        ModuleSymbol = moduleSymbol;
        Bodies = bodies;
        Writer = writer;
    }

    public ModuleSymbol ModuleSymbol { get; }
    public FrozenDictionary<SourceFunctionSymbol, ControlFlowGraph> Bodies { get; }
    public TextWriter Writer { get; }

    public static C99Backend Emit(
        ModuleSymbol moduleSymbol,
        FrozenDictionary<SourceFunctionSymbol, ControlFlowGraph> bodies,
        TextWriter writer
    )
    {
        var backend = new C99Backend(moduleSymbol, bodies, writer);
        backend.EmitModule();
        return backend;
    }

    private void EmitModule()
    {
        // C doesn't support forward-references, so we have to emit in order
        var structures = ModuleSymbol.GetMembers<StructureSymbol>();
        var emitted = new HashSet<IModuleMemberSymbol>(structures.Count());
        foreach (var structure in structures)
        {
            foreach (var field in structure.Fields)
            {
                if (field.Type.NativeType != NativeType.None)
                    continue;

                var fieldTypeAsStructure = (StructureSymbol)field.Type;
                if (emitted.Contains(fieldTypeAsStructure))
                    continue;

                EmitStructure(fieldTypeAsStructure);
                emitted.Add(fieldTypeAsStructure);
            }

            EmitStructure(structure);
        }

        var functions = ModuleSymbol.GetMembers<SourceFunctionSymbol>();
        foreach (var functionSymbol in functions)
            EmitFunctionSignature(functionSymbol);

        foreach (var functionSymbol in functions)
        {
            if (functionSymbol.IsExtern)
                continue;

            EmitFunction(functionSymbol);
        }
    }

    private void EmitStructure(StructureSymbol structureSymbol)
    {
        _output.Write("struct ");
        _output.WriteLine(structureSymbol.Name);
        _output.WriteLine("{");
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
        switch (typeSymbol.NativeType)
        {
            case NativeType.None:
                _output.Write("struct ");
                _output.Write(typeSymbol.Name);
                break;
            case NativeType.Unit:
                _output.Write("void");
                break;
            default:
                _output.Write(typeSymbol.Name);
                break;
        }
    }

    private void EmitFunction(SourceFunctionSymbol functionSymbol)
    {
        // TODO: Handle exported

        EmitFunctionSignature(functionSymbol);

        _output.WriteLine();
        _output.Write('{');
        _output.Indent++;

        var cfg = Bodies[functionSymbol];
        foreach (var statement in cfg.Statements)
            EmitStatement(statement);

        _output.Indent--;
        _output.Write('}');
    }

    private void EmitFunctionSignature(SourceFunctionSymbol functionSymbol)
    {
        EmitTypeReference(functionSymbol.ReturnType);
        _output.Write(' ');
        _output.Write(functionSymbol.ExternName ?? functionSymbol.Name);
        _output.Write('(');

        var isFirst = true;
        foreach (var parameter in functionSymbol.Parameters)
        {
            if (!isFirst)
                _output.Write(", ");

            if (!parameter.IsMutable)
                _output.Write("const ");

            EmitTypeReference(parameter.Type);
            _output.Write(' ');
            _output.Write(parameter.Name);

            isFirst = false;
        }

        _output.Write(')');

        if (functionSymbol.IsExtern)
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
        _output.Write(statement.Label.Name);
        _output.WriteLine(':');
        _output.Indent++;
    }

    private void EmitGotoStatement(BoundGotoStatement statement)
    {
        _output.Write("goto ");
        _output.Write(statement.Target.Name);
        _output.WriteLine(';');
    }

    private void EmitConditionalGotoStatement(BoundConditionalGotoStatement statement)
    {
        _output.Write("if (");
        if (statement.BranchIfFalse)
            _output.Write('!');

        EmitExpression(statement.Condition);
        _output.WriteLine(')');
        _output.Indent++;
        _output.Write("goto ");
        _output.Write(statement.Target.Name);
        _output.WriteLine(';');
        _output.Indent--;
    }

    private void EmitLocalDeclaration(BoundLocalDeclaration statement)
    {
        EmitTypeReference(statement.Local.Type);
        _output.Write(' ');
        _output.Write(statement.Local.Name);
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
            case BoundNodeKind.BlockExpression:
                break;
            case BoundNodeKind.LiteralExpression:
                break;
            case BoundNodeKind.CallExpression:
                break;
            case BoundNodeKind.AccessExpression:
                break;
            case BoundNodeKind.StructureLiteralExpression:
                break;
            case BoundNodeKind.NameExpression:
                break;
            case BoundNodeKind.BinaryExpression:
                break;
            case BoundNodeKind.AssignmentExpression:
                break;
            default:
                throw new UnreachableException();
        }
    }
}
