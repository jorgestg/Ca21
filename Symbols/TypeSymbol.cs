using System.Text;
using Antlr4.Runtime;

namespace Ca21.Symbols;

internal abstract class TypeSymbol : Symbol
{
    public static readonly TypeSymbol BadType = new NativeTypeSymbol("???");
    public static readonly TypeSymbol Unit = new NativeTypeSymbol("unit");
    public static readonly TypeSymbol Int32 = new NativeTypeSymbol("int32");
    public static readonly TypeSymbol Bool = new NativeTypeSymbol("bool");
    public static readonly TypeSymbol String = new NativeTypeSymbol("string");

    private sealed class NativeTypeSymbol(string name) : TypeSymbol
    {
        public override ParserRuleContext Context => throw new InvalidOperationException();
        public override string Name { get; } = name;
    }
}

internal sealed class FunctionTypeSymbol(FunctionSymbol functionSymbol) : TypeSymbol
{
    public override ParserRuleContext Context => ParserRuleContext.EMPTY;

    private string? _name;
    public override string Name
    {
        get
        {
            if (_name != null)
                return _name;

            if (FunctionSymbol.Parameters.Length == 1)
                return _name = $"func ({FunctionSymbol.Parameters[0].Type.Name}) {FunctionSymbol.ReturnType.Name}";

            var stringBuilder = new StringBuilder();
            stringBuilder.Append("func (");

            foreach (var parameter in FunctionSymbol.Parameters)
            {
                if (stringBuilder.Length > 0)
                    stringBuilder.Append(", ");

                stringBuilder.Append(parameter.Type.Name);
            }

            stringBuilder.Append(") ").Append(FunctionSymbol.ReturnType.Name);
            return _name = stringBuilder.ToString();
        }
    }

    public FunctionSymbol FunctionSymbol { get; } = functionSymbol;

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
            return true;

        if (obj is not FunctionTypeSymbol other)
            return false;

        if (FunctionSymbol.ReturnType.Equals(other.FunctionSymbol.ReturnType))
            return false;

        if (FunctionSymbol.Parameters.Length != other.FunctionSymbol.Parameters.Length)
            return false;

        for (var i = 0; i < FunctionSymbol.Parameters.Length; i++)
        {
            if (FunctionSymbol.Parameters[i].Type.Equals(other.FunctionSymbol.Parameters[i].Type))
                return false;
        }

        return true;
    }

    public override int GetHashCode() => base.GetHashCode();
}
