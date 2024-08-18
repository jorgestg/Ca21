using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Text;
using Antlr4.Runtime;
using Ca21.Diagnostics;
using static Ca21.Antlr.Ca21Parser;

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

internal sealed class StructureSymbol : TypeSymbol, IModuleMemberSymbol
{
    public StructureSymbol(StructureDefinitionContext context, ModuleSymbol module)
    {
        Context = context;
        ContainingModule = module;
        if (context._Fields.Count == 0)
        {
            Fields = [];
            FieldMap = FrozenDictionary<string, FieldSymbol>.Empty;
            return;
        }

        var diagnostics = new DiagnosticList();
        var fieldsBuilder = new ArrayBuilder<FieldSymbol>(context._Fields.Count);
        var mapBuilder = new Dictionary<string, FieldSymbol>(context._Fields.Count);
        foreach (var fieldContext in Context._Fields)
        {
            var fieldSymbol = new FieldSymbol(fieldContext, this);
            if (!mapBuilder.TryAdd(fieldSymbol.Name, fieldSymbol))
                diagnostics.Add(fieldContext.Name, DiagnosticMessages.NameIsAlreadyDefined(fieldSymbol.Name));

            fieldsBuilder.Add(fieldSymbol);
        }

        Diagnostics = diagnostics.GetImmutableArray();
        Fields = fieldsBuilder.MoveToImmutable();
        FieldMap = mapBuilder.ToFrozenDictionary();
    }

    public override StructureDefinitionContext Context { get; }
    public override string Name => Context.Name.Text;
    public override ImmutableArray<Diagnostic> Diagnostics { get; }

    public ModuleSymbol ContainingModule { get; }

    public ImmutableArray<FieldSymbol> Fields { get; }
    public FrozenDictionary<string, FieldSymbol> FieldMap { get; }
}
