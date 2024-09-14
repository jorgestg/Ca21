namespace Ca21.Binding;

internal enum BoundNodeKind
{
    NopStatement,
    LabelStatement,
    GotoStatement,
    ConditionalGotoStatement,
    LocalDeclaration,
    WhileStatement,
    ReturnStatement,
    Block,
    ExpressionStatement,
    BlockExpression,
    LiteralExpression,
    CallExpression,
    AccessExpression,
    StructureLiteralExpression,
    NameExpression,
    BinaryExpression,
    AssignmentExpression
}

internal abstract class BoundNode
{
    public abstract BoundNodeKind Kind { get; }
}