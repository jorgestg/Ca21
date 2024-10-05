using System.Collections.Frozen;
using Antlr4.Runtime;

namespace Ca21.Symbols;

internal enum TypeKind
{
    None,
    Structure,
    Enumeration,
    Function,
    Void,
    Int32,
    Int64,
    Bool,
    String
}

internal abstract class TypeSymbol : Symbol
{
    public static new readonly TypeSymbol Missing = new NativeTypeSymbol("???", TypeKind.None);
    public static readonly TypeSymbol Void = new NativeTypeSymbol("void", TypeKind.Void);
    public static readonly TypeSymbol Int32 = new NativeTypeSymbol("int32", TypeKind.Int32);
    public static readonly TypeSymbol Int64 = new NativeTypeSymbol("int64", TypeKind.Int64);
    public static readonly TypeSymbol Bool = new NativeTypeSymbol("bool", TypeKind.Bool);
    public static readonly TypeSymbol String = new NativeTypeSymbol("string", TypeKind.String);

    public override SymbolKind SymbolKind => SymbolKind.Type;
    public virtual TypeKind TypeKind => TypeKind.None;

    public virtual bool TryGetMember(string name, out TypeMemberSymbol member)
    {
        member = TypeMemberSymbol.Missing;
        return false;
    }

    public static TypeSymbol? Unify(TypeSymbol a, TypeSymbol b)
    {
        if (a == Missing || b == Missing)
            return Missing;

        if (a.Equals(b))
            return a;

        return (a.TypeKind, b.TypeKind) switch
        {
            (TypeKind.Int32, TypeKind.Int64) => Int64,
            (TypeKind.Int64, TypeKind.Int32) => Int64,
            _ => null
        };
    }

    private sealed class NativeTypeSymbol(string name, TypeKind nativeType) : TypeSymbol
    {
        public override ParserRuleContext Context => throw new InvalidOperationException();
        public override string Name { get; } = name;
        public override TypeKind TypeKind { get; } = nativeType;
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

            if (FunctionSymbol.Parameters.Length == 0)
                return _name = $"func () {FunctionSymbol.ReturnType.Name}";

            if (FunctionSymbol.Parameters.Length == 1)
                return _name = $"func ({FunctionSymbol.Parameters[0].Type.Name}) {FunctionSymbol.ReturnType.Name}";

            const string start = "func (";
            const string end = ") ";
            var length =
                start.Length
                + FunctionSymbol.Parameters.Sum(p => p.Type.Name.Length)
                + (FunctionSymbol.Parameters.Length - 1) * 2 // ", "
                + end.Length
                + FunctionSymbol.ReturnType.Name.Length;

            return _name = string.Create(
                length,
                this,
                static (buffer, @this) =>
                {
                    start.CopyTo(buffer);
                    buffer = buffer.Slice(start.Length);

                    for (var i = 0; i < @this.FunctionSymbol.Parameters.Length; i++)
                    {
                        var parameter = @this.FunctionSymbol.Parameters[i];
                        parameter.Type.Name.CopyTo(buffer.Slice(i));
                        buffer = buffer.Slice(parameter.Type.Name.Length);

                        if (i < @this.FunctionSymbol.Parameters.Length - 1)
                        {
                            buffer[0] = ',';
                            buffer[1] = ' ';
                            buffer = buffer.Slice(2);
                        }
                    }

                    end.CopyTo(buffer);
                    buffer = buffer.Slice(end.Length);

                    @this.FunctionSymbol.ReturnType.Name.CopyTo(buffer);
                }
            );
        }
    }

    public override TypeKind TypeKind => TypeKind.Function;

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

internal abstract class TypeMemberSymbol : Symbol
{
    public static new readonly TypeMemberSymbol Missing = new MissingTypeMemberSymbol();

    public abstract TypeSymbol ContainingType { get; }

    private sealed class MissingTypeMemberSymbol : TypeMemberSymbol
    {
        public override SymbolKind SymbolKind => SymbolKind.None;
        public override ParserRuleContext Context => throw new InvalidOperationException();
        public override string Name => throw new InvalidOperationException();
        public override TypeSymbol ContainingType => throw new InvalidOperationException();
    }
}
