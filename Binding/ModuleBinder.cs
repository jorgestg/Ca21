using System.Diagnostics;
using Ca21.Diagnostics;
using Ca21.Symbols;
using static Ca21.Antlr.Ca21Parser;

namespace Ca21.Binding;

internal sealed class ModuleBinder(ModuleSymbol moduleSymbol) : Binder
{
    private readonly ModuleSymbol _moduleSymbol = moduleSymbol;

    public override Binder Parent => throw new InvalidOperationException();

    public override Symbol? Lookup(string name) => (Symbol?)_moduleSymbol.MemberMap.GetValueOrDefault(name);

    public override TypeSymbol BindType(TypeReferenceContext context, DiagnosticList diagnostics)
    {
        if (context is KeywordTypeReferenceContext keywordReference)
        {
            return keywordReference.TypeKeyword.Keyword.Type switch
            {
                Int32Keyword => TypeSymbol.Int32,
                BoolKeyword => TypeSymbol.Bool,
                StringKeyword => TypeSymbol.String,
                _ => throw new UnreachableException()
            };
        }

        var nameReference = (SimpleNameTypeReferenceContext)context;
        if (!_moduleSymbol.MemberMap.TryGetValue(nameReference.Name.Text, out var member))
        {
            diagnostics.Add(nameReference, DiagnosticMessages.NameNotFound(nameReference.Name.Text));
            return TypeSymbol.Missing;
        }

        if (member.Kind != SymbolKind.Type)
        {
            diagnostics.Add(nameReference, DiagnosticMessages.NameIsNotAType(nameReference.Name.Text));
            return TypeSymbol.Missing;
        }

        return (TypeSymbol)member;
    }
}
