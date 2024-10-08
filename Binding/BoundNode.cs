namespace Ca21.Binding;

internal enum BoundNodeKind
{
    NopStatement,
    LabelStatement,
    GotoStatement,
    ConditionalGotoStatement,
    LocalDeclaration,
    IfStatement,
    WhileStatement,
    ReturnStatement,
    ExpressionStatement,
    Block,
    BlockExpression,
    CastExpression,
    LiteralExpression,
    CallExpression,
    AccessExpression,
    StructureLiteralExpression,
    UnaryExpression,
    NameExpression,
    BinaryExpression,
    AssignmentExpression
}

internal abstract class BoundNode
{
    public abstract BoundNodeKind Kind { get; }
}
