using Antlr4.Runtime;
using Ca21.Binding;
using static Ca21.Antlr.Ca21Parser;

namespace Ca21.Symbols;

internal abstract class FunctionSymbol : Symbol
{
    public static new readonly FunctionSymbol Missing = new MissingFunctionSymbol();

    public abstract ModuleSymbol Module { get; }

    public abstract TypeSymbol ReturnType { get; }

    private sealed class MissingFunctionSymbol : FunctionSymbol
    {
        public override ParserRuleContext Context => ParserRuleContext.EMPTY;
        public override ModuleSymbol Module => throw new InvalidOperationException();
        public override TypeSymbol ReturnType => TypeSymbol.BadType;
        public override string Name => "???";
    }
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
    public override string Name => Context.Signature.Name.Text;
    public override FunctionBinder Binder { get; }

    public override ModuleSymbol Module { get; }

    private TypeSymbol? _returnType;
    public override TypeSymbol ReturnType => _returnType ??= Binder.BindType(Context.Signature.ReturnType);
}