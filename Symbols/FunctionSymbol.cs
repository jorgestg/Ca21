using Ca21.Binding;
using static Ca21.Antlr.Ca21Parser;

namespace Ca21.Symbols;

internal abstract class FunctionSymbol : Symbol
{
    public abstract ModuleSymbol Module { get; }

    public abstract TypeSymbol ReturnType { get; }
}

internal sealed class SourceFunctionSymbol : FunctionSymbol
{
    public SourceFunctionSymbol(FunctionDefinitionContext context, ModuleSymbol module)
    {
        Context = context;
        Binder = new FunctionBinder(this);
        Module = module;
    }

    public override FunctionDefinitionContext Context { get; }
    public override string Name => Context.Name.Text;
    public override FunctionBinder Binder { get; }

    public override ModuleSymbol Module { get; }

    private TypeSymbol? _returnType;
    public override TypeSymbol ReturnType => _returnType ??= Binder.BindType(Context.ReturnType);
}
