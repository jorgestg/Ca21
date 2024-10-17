using static Ca21.Antlr.Ca21Parser;

namespace Ca21.Symbols;

internal sealed class TypeReferenceSymbol(ReferenceTypeNameContext context, TypeSymbol innerType) : TypeSymbol
{
    public override ReferenceTypeNameContext Context { get; } = context;

    private string? _name;
    public override string Name => _name ??= $"&{InnerType.Name}";

    public override TypeKind TypeKind => TypeKind.Reference;

    public TypeSymbol InnerType { get; } = innerType;

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
            return true;

        return obj is TypeReferenceSymbol other && Equals(InnerType, other.InnerType);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}

internal sealed class TypeSliceSymbol(SliceTypeNameContext context, TypeSymbol innerType, int? fixedSize) : TypeSymbol
{
    public override SliceTypeNameContext Context { get; } = context;

    private string? _name;
    public override string Name => _name ??= $"[{FixedSize}]{InnerType.Name}";

    public override TypeKind TypeKind => TypeKind.Reference;

    public TypeSymbol InnerType { get; } = innerType;
    public int? FixedSize { get; } = fixedSize;

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(this, obj))
            return true;

        return obj is TypeSliceSymbol other && FixedSize == other.FixedSize && Equals(InnerType, other.InnerType);
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
