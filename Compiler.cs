using System.Collections.Frozen;
using System.Collections.Immutable;
using Ca21.Binding;
using Ca21.Diagnostics;
using Ca21.Symbols;

namespace Ca21;

internal sealed class Compiler
{
    private readonly DiagnosticList _diagnosticsBuilder = new();
    private readonly Dictionary<SourceFunctionSymbol, ControlFlowGraph> _bodiesBuilder = new();
    private readonly HashSet<string> _constantsBuilder = new();

    private Compiler(PackageSymbol package)
    {
        Package = package;
    }

    public PackageSymbol Package { get; }

    public ImmutableArray<Diagnostic> Diagnostics => _diagnosticsBuilder.GetImmutableArray();

    private FrozenDictionary<SourceFunctionSymbol, ControlFlowGraph>? _bodies;
    public FrozenDictionary<SourceFunctionSymbol, ControlFlowGraph> Bodies =>
        _bodies ??= _bodiesBuilder.ToFrozenDictionary();

    private FrozenSet<string>? _constants;
    public FrozenSet<string> Constants => _constants ??= _constantsBuilder.ToFrozenSet();

    public static Compiler Compile(PackageSymbol packageSymbol)
    {
        var compiler = new Compiler(packageSymbol);
        foreach (var module in packageSymbol.Modules)
            compiler.CompileModule(module);

        return compiler;
    }

    private void CompileModule(ModuleSymbol moduleSymbol)
    {
        _diagnosticsBuilder.AddRange(moduleSymbol.Diagnostics);

        foreach (var structureSymbol in moduleSymbol.GetMembers<StructureSymbol>())
            _diagnosticsBuilder.AddRange(structureSymbol.Diagnostics);

        foreach (var enumerationSymbol in moduleSymbol.GetMembers<EnumerationSymbol>())
            _diagnosticsBuilder.AddRange(enumerationSymbol.Diagnostics);

        foreach (var functionSymbol in moduleSymbol.GetMembers<SourceFunctionSymbol>())
            CompileFunction(functionSymbol);
    }

    private void CompileFunction(SourceFunctionSymbol functionSymbol)
    {
        var cfg = functionSymbol.Binder.BindBody(_diagnosticsBuilder);
        if (cfg == null)
            return;

        foreach (var statement in cfg.Statements)
            CompileStatement(statement);

        _diagnosticsBuilder.AddRange(functionSymbol.Diagnostics);
        _bodiesBuilder.Add(functionSymbol, cfg);
    }

    private void CompileStatement(BoundStatement statement)
    {
        switch (statement.Kind)
        {
            case BoundNodeKind.ConditionalGotoStatement:
                CompileConditionalGotoStatement((BoundConditionalGotoStatement)statement);
                break;
            case BoundNodeKind.LocalDeclaration:
                CompileLocalDeclaration((BoundLocalDeclaration)statement);
                break;
            case BoundNodeKind.ReturnStatement:
                CompileReturnStatement((BoundReturnStatement)statement);
                break;
            case BoundNodeKind.ExpressionStatement:
                CompileExpressionStatement((BoundExpressionStatement)statement);
                break;
        }
    }

    private void CompileConditionalGotoStatement(BoundConditionalGotoStatement statement)
    {
        CompileExpression(statement.Condition);
    }

    private void CompileLocalDeclaration(BoundLocalDeclaration statement)
    {
        if (statement.Initializer == null)
            return;

        CompileExpression(statement.Initializer);
    }

    private void CompileReturnStatement(BoundReturnStatement statement)
    {
        if (statement.Expression == null)
            return;

        CompileExpression(statement.Expression);
    }

    private void CompileExpressionStatement(BoundExpressionStatement statement)
    {
        CompileExpression(statement.Expression);
    }

    private void CompileExpression(BoundExpression expression)
    {
        switch (expression.Kind)
        {
            case BoundNodeKind.LiteralExpression:
                CompileLiteralExpression((BoundLiteralExpression)expression);
                break;
            case BoundNodeKind.CallExpression:
                CompileCallExpression((BoundCallExpression)expression);
                break;
            case BoundNodeKind.StructureLiteralExpression:
                CompileStructureLiteralExpression((BoundStructureLiteralExpression)expression);
                break;
            case BoundNodeKind.BinaryExpression:
                CompileBinaryExpression((BoundBinaryExpression)expression);
                break;
            case BoundNodeKind.AssignmentExpression:
                CompileAssignmentExpression((BoundAssignmentExpression)expression);
                break;
        }
    }

    private void CompileLiteralExpression(BoundLiteralExpression expression)
    {
        if (expression.Value is string s)
            _constantsBuilder.Add(s);
    }

    private void CompileCallExpression(BoundCallExpression expression)
    {
        foreach (var argument in expression.Arguments)
            CompileExpression(argument);
    }

    private void CompileStructureLiteralExpression(BoundStructureLiteralExpression expression)
    {
        foreach (var member in expression.FieldInitializers)
            CompileExpression(member.Expression);
    }

    private void CompileBinaryExpression(BoundBinaryExpression expression)
    {
        CompileExpression(expression.Left);
        CompileExpression(expression.Right);
    }

    private void CompileAssignmentExpression(BoundAssignmentExpression expression)
    {
        CompileExpression(expression.Expression);
    }
}
