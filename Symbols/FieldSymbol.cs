using System.Collections.Immutable;
using Ca21.Diagnostics;
using static Ca21.Antlr.Ca21Parser;

namespace Ca21.Symbols;

internal sealed class FieldSymbol(FieldDefinitionContext context, StructureSymbol containingType) : Symbol
{
    public override FieldDefinitionContext Context { get; } = context;

    public override string Name => Context.Name.Text;

    private TypeSymbol? _type;
    public override TypeSymbol Type => _type ??= ContainingType.Binder.BindType(Context.Type, _diagnostics);

    private readonly DiagnosticList _diagnostics = new();
    public override ImmutableArray<Diagnostic> Diagnostics => _diagnostics.GetImmutableArray();

    public StructureSymbol ContainingType { get; } = containingType;
}
