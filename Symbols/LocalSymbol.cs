using Antlr4.Runtime;
using static Ca21.Antlr.Ca21Parser;

namespace Ca21.Symbols;

internal abstract class LocalSymbol : Symbol
{
    public override SymbolKind SymbolKind => SymbolKind.Local;
    public abstract bool IsMutable { get; }
}

internal sealed class LabelSymbol(ParserRuleContext context, string name) : LocalSymbol
{
    public override ParserRuleContext Context { get; } = context;
    public override string Name { get; } = name;
    public override bool IsMutable => throw new InvalidOperationException();
}

internal sealed class SourceLocalSymbol(LocalDeclarationContext context, TypeSymbol type) : LocalSymbol
{
    public override LocalDeclarationContext Context { get; } = context;
    public override string Name => Context.Name.Text;
    public override TypeSymbol Type { get; } = type;
    public override bool IsMutable => Context.MutModifier != null;
}

internal sealed class SourceParameterSymbol(
    ParameterDefinitionContext context,
    SourceFunctionSymbol functionSymbol,
    TypeSymbol type
) : LocalSymbol
{
    public override ParameterDefinitionContext Context { get; } = context;
    public override string Name => Context.Name.Text;
    public override TypeSymbol Type { get; } = type;
    public override bool IsMutable => Context.MutModifier != null;
    public SourceFunctionSymbol ContainingFunction { get; } = functionSymbol;
}
