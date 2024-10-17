using System.Collections.Frozen;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Ca21.Binding;
using Ca21.Diagnostics;
using static Ca21.Antlr.Ca21Parser;

namespace Ca21.Symbols;

internal sealed class StructureSymbol : TypeSymbol, IMemberSymbol
{
    public StructureSymbol(StructureDefinitionContext context, ModuleSymbol module, FileBinder fileBinder)
    {
        Context = context;
        Binder = new StructureBinder(fileBinder, this);
        ContainingSymbol = module;
    }

    public override StructureDefinitionContext Context { get; }
    public override string Name => Context.Name.Text;
    public override TypeKind TypeKind => TypeKind.Structure;
    public IContainingSymbol ContainingSymbol { get; }

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

    public override bool TryGetMember(string name, out IMemberSymbol member)
    {
        if (FieldMap.TryGetValue(name, out var field))
        {
            member = field;
            return true;
        }

        return base.TryGetMember(name, out member);
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
            var fieldSymbol = new FieldSymbol(fieldContext, this, type);
            if (!mapBuilder.TryAdd(fieldSymbol.Name, fieldSymbol))
                _diagnostics.Add(fieldContext.Name, DiagnosticMessages.NameIsAlreadyDefined(fieldSymbol));

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
            _diagnostics.Add(source.Context, DiagnosticMessages.CycleDetected(source));
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

internal sealed class FieldSymbol(FieldDefinitionContext context, StructureSymbol structure, TypeSymbol type)
    : Symbol,
        IMemberSymbol
{
    public override SymbolKind SymbolKind => SymbolKind.Field;
    public override FieldDefinitionContext Context { get; } = context;
    public override string Name => Context.Name.Text;
    public override TypeSymbol Type { get; } = type;
    public IContainingSymbol ContainingSymbol { get; } = structure;
}
