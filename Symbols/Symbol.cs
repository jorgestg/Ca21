using Antlr4.Runtime;
using Ca21.Binding;
using static Ca21.Antlr.Ca21Parser;

namespace Ca21.Symbols;

internal abstract class Symbol
{
    public static readonly Symbol Missing = new MissingSymbol();

    public abstract ParserRuleContext Context { get; }
    public abstract string Name { get; }
    public virtual TypeSymbol Type => TypeSymbol.BadType;
    public virtual Binder Binder => throw new InvalidOperationException();

    private sealed class MissingSymbol : Symbol
    {
        public override ParserRuleContext Context => ParserRuleContext.EMPTY;
        public override string Name => "???";
    }
}

internal sealed class SourceFunctionSymbol : Symbol
{
    public SourceFunctionSymbol(FunctionDefinitionContext context)
    {
        Context = context;
        Binder = new FunctionBinder(this);
    }

    public override FunctionDefinitionContext Context { get; }
    public override string Name => Context.Name.Text;
    public override FunctionBinder Binder { get; }

    private TypeSymbol? _returnType;
    public TypeSymbol ReturnType => _returnType ??= Binder.BindType(Context.ReturnType);
}

internal abstract class LocalSymbol : Symbol;

internal sealed class LabelSymbol(ParserRuleContext context, string name) : LocalSymbol
{
    public override ParserRuleContext Context { get; } = context;
    public override string Name { get; } = name;
}

internal sealed class SyntheticLocalSymbol(BoundExpression originalNode) : LocalSymbol
{
    public override ParserRuleContext Context => OriginalExpression.Context;
    public override TypeSymbol Type => OriginalExpression.Type;
    public override string Name => "tmp";
    public BoundExpression OriginalExpression { get; } = originalNode;
}

internal sealed class SourceLocalSymbol(LocalDeclarationContext context, TypeSymbol type) : LocalSymbol
{
    public override LocalDeclarationContext Context { get; } = context;
    public override string Name => Context.Name.Text;
    public bool IsMutable => Context.MutModifier != null;
    public override TypeSymbol Type { get; } = type;
}
