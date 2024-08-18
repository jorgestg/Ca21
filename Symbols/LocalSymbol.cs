using System.Collections.Immutable;
using Ca21.Diagnostics;
using static Ca21.Antlr.Ca21Parser;

namespace Ca21.Symbols;

internal abstract class LocalSymbol : Symbol;

internal sealed class SourceLocalSymbol(LocalDeclarationContext context, TypeSymbol type) : LocalSymbol
{
    public override LocalDeclarationContext Context { get; } = context;
    public override string Name => Context.Name.Text;
    public bool IsMutable => Context.MutModifier != null;
    public override TypeSymbol Type { get; } = type;
}

internal sealed class SourceParameterSymbol(ParameterDefinitionContext context, SourceFunctionSymbol functionSymbol)
    : LocalSymbol
{
    public override ParameterDefinitionContext Context { get; } = context;
    public override string Name => Context.Name.Text;

    private TypeSymbol? _type;
    public override TypeSymbol Type => _type ??= ContainingFunction.Binder.BindType(Context.Type, _diagnostics);

    private readonly DiagnosticList _diagnostics = new();
    public override ImmutableArray<Diagnostic> Diagnostics => _diagnostics.GetImmutableArray();

    public SourceFunctionSymbol ContainingFunction { get; } = functionSymbol;
    public bool IsMutable => Context.MutModifier != null;
}
