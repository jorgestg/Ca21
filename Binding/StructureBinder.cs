using Ca21.Diagnostics;
using Ca21.Symbols;
using static Ca21.Antlr.Ca21Parser;

namespace Ca21.Binding;

internal sealed class StructureBinder(StructureSymbol structureSymbol) : Binder
{
    private readonly StructureSymbol _structureSymbol = structureSymbol;
    private bool _bindingType;

    public override Binder Parent => _structureSymbol.ContainingModule.Binder;

    public override Symbol? Lookup(string name) =>
        _structureSymbol.FieldMap.GetValueOrDefault(name) ?? base.Lookup(name);

    public TypeSymbol BindFieldType(FieldDefinitionContext context, DiagnosticList diagnostics)
    {
        if (_bindingType)
        {
            diagnostics.Add(context.Type, DiagnosticMessages.CycleDetected(context.Name.Text, _structureSymbol));
            return TypeSymbol.Missing;
        }

        _bindingType = true;
        var type = BindType(context.Type, diagnostics);
        _bindingType = false;
        return type;
    }
}
