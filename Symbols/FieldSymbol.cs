using Antlr4.Runtime;
using static Ca21.Antlr.Ca21Parser;

namespace Ca21.Symbols;

internal abstract class FieldSymbol : Symbol
{
    public static new readonly FieldSymbol Missing = new MissingFieldSymbol();

    private sealed class MissingFieldSymbol : FieldSymbol
    {
        public override ParserRuleContext Context => ParserRuleContext.EMPTY;
        public override string Name => "???";
    }
}

internal sealed class SourceFieldSymbol(FieldDefinitionContext context, StructureSymbol containingType, TypeSymbol type)
    : FieldSymbol
{
    public override FieldDefinitionContext Context { get; } = context;
    public override string Name => Context.Name.Text;
    public override TypeSymbol Type { get; } = type;

    public StructureSymbol ContainingType { get; } = containingType;
}
