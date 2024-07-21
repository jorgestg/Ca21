using Antlr4.Runtime;

namespace Ca21.Symbols;

internal abstract class TypeSymbol : Symbol
{
    public static readonly TypeSymbol BadType = new NativeTypeSymbol("???");
    public static readonly TypeSymbol Never = new NativeTypeSymbol("never");
    public static readonly TypeSymbol Unit = new NativeTypeSymbol("unit");
    public static readonly TypeSymbol Int32 = new NativeTypeSymbol("int32");
    public static readonly TypeSymbol Bool = new NativeTypeSymbol("bool");

    private sealed class NativeTypeSymbol(string name) : TypeSymbol
    {
        public override ParserRuleContext Context => throw new InvalidOperationException();
        public override string Name { get; } = name;
    }
}
