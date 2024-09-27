using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Antlr4.Runtime;
using Ca21.Binding;
using Ca21.Diagnostics;
using static Ca21.Antlr.Ca21Parser;

namespace Ca21.Symbols;

internal enum NativeType
{
    None,
    Unit,
    Int32,
    Int64,
    Bool,
    String
}

internal abstract class TypeSymbol : Symbol
{
    public static new readonly TypeSymbol Missing = new NativeTypeSymbol("???", NativeType.None);
    public static readonly TypeSymbol Unit = new NativeTypeSymbol("unit", NativeType.Unit);
    public static readonly TypeSymbol Int32 = new NativeTypeSymbol("int32", NativeType.Int32);
    public static readonly TypeSymbol Int64 = new NativeTypeSymbol("int64", NativeType.Int64);
    public static readonly TypeSymbol Bool = new NativeTypeSymbol("bool", NativeType.Bool);
    public static readonly TypeSymbol String = new NativeTypeSymbol("string", NativeType.String);

    public override SymbolKind Kind => SymbolKind.Type;
    public virtual NativeType NativeType => NativeType.None;

    public static TypeSymbol? Unify(TypeSymbol a, TypeSymbol b)
    {
        if (a == Missing || b == Missing)
            return Missing;

        if (a.Equals(b))
            return a;

        return (a.NativeType, b.NativeType) switch
        {
            (NativeType.Int32, NativeType.Int64) => Int64,
            (NativeType.Int64, NativeType.Int32) => Int64,
            _ => null
        };
    }

    private sealed class NativeTypeSymbol(string name, NativeType nativeType) : TypeSymbol
    {
        public override ParserRuleContext Context => throw new InvalidOperationException();
        public override string Name { get; } = name;
        public override NativeType NativeType { get; } = nativeType;
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
        Binder = new StructureBinder(this);
        ContainingModule = module;
    }

    public override StructureDefinitionContext Context { get; }
    public override string Name => Context.Name.Text;

    public ModuleSymbol ContainingModule { get; }

    /// <summary>
    /// Cycle check status: Not started = null, Running = false, Done = true
    /// </summary>
    private bool? _cycleCheckDone;
    private readonly DiagnosticList _diagnostics = new();
    public ImmutableArray<Diagnostic> Diagnostics
    {
        get
        {
            if (_cycleCheckDone == null)
                CheckForCycles();

            return _diagnostics.GetImmutableArray();
        }
    }

    public StructureBinder Binder { get; }

    private ImmutableArray<FieldSymbol> _fields;
    public ImmutableArray<FieldSymbol> Fields
    {
        get
        {
            if (_fields.IsDefault)
                CreateFields();

            return _fields;
        }
    }

    private FrozenDictionary<string, FieldSymbol>? _fieldMap;
    public FrozenDictionary<string, FieldSymbol> FieldMap
    {
        get
        {
            if (_fieldMap == null)
                CreateFields();

            return _fieldMap;
        }
    }

    [MemberNotNull(nameof(_fieldMap))]
    private void CreateFields()
    {
        if (Context._Fields.Count == 0)
        {
            _fields = [];
            _fieldMap = FrozenDictionary<string, FieldSymbol>.Empty;
            return;
        }

        var fieldsBuilder = new ArrayBuilder<FieldSymbol>(Context._Fields.Count);
        var mapBuilder = new Dictionary<string, FieldSymbol>(Context._Fields.Count);
        foreach (var fieldContext in Context._Fields)
        {
            var type = Binder.BindType(fieldContext.Type, _diagnostics);
            var fieldSymbol = new SourceFieldSymbol(fieldContext, this, type);
            if (!mapBuilder.TryAdd(fieldSymbol.Name, fieldSymbol))
                _diagnostics.Add(fieldContext.Name, DiagnosticMessages.NameIsAlreadyDefined(fieldSymbol.Name));

            fieldsBuilder.Add(fieldSymbol);
        }

        _fields = fieldsBuilder.MoveToImmutable();
        _fieldMap = mapBuilder.ToFrozenDictionary();
    }

    private void CheckForCycles(FieldSymbol? source = null)
    {
        // If this method got called while we're checking for cycles, means we've found a cycle.
        if (_cycleCheckDone == false)
        {
            Debug.Assert(source != null);
            _diagnostics.Add(source.Context, DiagnosticMessages.CycleDetected((SourceFieldSymbol)source));
            return;
        }

        _cycleCheckDone = false;

        foreach (var field in Fields)
        {
            if (field.Type is StructureSymbol structure)
                structure.CheckForCycles(field);
        }

        _cycleCheckDone = true;
    }
}
