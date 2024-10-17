using System.Diagnostics;
using Ca21.Diagnostics;
using Ca21.Symbols;
using static Ca21.Antlr.Ca21Parser;

namespace Ca21.Binding;

internal sealed class ModuleBinder(ModuleSymbol moduleSymbol) : Binder
{
    private readonly Dictionary<CompilationUnitContext, FileBinder> _fileBinders = new();

    public override Binder Parent => throw new InvalidOperationException();
    public ModuleSymbol ModuleSymbol { get; } = moduleSymbol;

    public override Symbol? Lookup(string name) => (Symbol?)ModuleSymbol.MemberMap.GetValueOrDefault(name);

    private static TypeSymbol BindTypeKeyword(KeywordTypeNameContext keywordReference)
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

    public override TypeSymbol BindType(TypeNameContext context, DiagnosticList diagnostics)
    {
        if (context is KeywordTypeNameContext keywordReference)
            return BindTypeKeyword(keywordReference);

        var nameReference = (SimpleNameTypeNameContext)context;
        if (!ModuleSymbol.MemberMap.TryGetValue(nameReference.Name.Text, out var member))
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

    public FileBinder GetFileBinder(CompilationUnitContext root)
    {
        if (_fileBinders.TryGetValue(root, out var fileBinder))
            return fileBinder;

        return _fileBinders[root] = new FileBinder(this, root);
    }
}
