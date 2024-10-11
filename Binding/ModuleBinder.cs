using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Ca21.Diagnostics;
using Ca21.Symbols;
using static Ca21.Antlr.Ca21Parser;

namespace Ca21.Binding;

internal sealed class ModuleBinder(ModuleSymbol moduleSymbol) : Binder
{
    private readonly ModuleSymbol _moduleSymbol = moduleSymbol;

    public override Binder Parent => throw new InvalidOperationException();

    public override Symbol? Lookup(string name) => (Symbol?)_moduleSymbol.MemberMap.GetValueOrDefault(name);

    public override bool TryBindType(TypeReferenceContext context, [MaybeNullWhen(false)] out TypeSymbol typeSymbol)
    {
        if (context is KeywordTypeReferenceContext keywordReference)
        {
            typeSymbol = BindTypeKeyword(keywordReference);
            return true;
        }

        var nameReference = (SimpleNameTypeReferenceContext)context;
        if (
            _moduleSymbol.MemberMap.TryGetValue(nameReference.Name.Text, out var member)
            && member.SymbolKind == SymbolKind.Type
        )
        {
            typeSymbol = (TypeSymbol)member;
            return true;
        }

        typeSymbol = null;
        return false;
    }

    private static TypeSymbol BindTypeKeyword(KeywordTypeReferenceContext keywordReference)
    {
        return keywordReference.TypeKeyword.Keyword.Type switch
        {
            Int32Keyword => TypeSymbol.Int32,
            Int64Keyword => TypeSymbol.Int64,
            USizeKeyword => TypeSymbol.USize,
            BoolKeyword => TypeSymbol.Bool,
            StringKeyword => TypeSymbol.String,
            _ => throw new UnreachableException()
        };
    }

    public override TypeSymbol BindType(TypeReferenceContext context, DiagnosticList diagnostics)
    {
        if (context is KeywordTypeReferenceContext keywordReference)
            return BindTypeKeyword(keywordReference);

        var nameReference = (SimpleNameTypeReferenceContext)context;
        if (!_moduleSymbol.MemberMap.TryGetValue(nameReference.Name.Text, out var member))
        {
            diagnostics.Add(nameReference, DiagnosticMessages.NameNotFound(nameReference.Name.Text));
            return TypeSymbol.Missing;
        }

        if (member.SymbolKind != SymbolKind.Type)
        {
            diagnostics.Add(nameReference, DiagnosticMessages.NameIsNotAType(nameReference.Name.Text));
            return TypeSymbol.Missing;
        }

        return (TypeSymbol)member;
    }
}
