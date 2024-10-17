using Ca21.Symbols;

namespace Ca21.Diagnostics;

internal static class DiagnosticMessages
{
    public static string ModuleNotFound(string name) => $"Module `{name}` not found";

    public static string TypeMismatch(TypeSymbol expected, TypeSymbol got) =>
        $"Type mismatch. Expected `{expected.Name}`, got `{got.Name}`";

    public const string ExpressionCannotBeUsedAsStatement = "Expression cannot be used as a statement";

    public static string NameNotFound(string name) => $"Name `{name}` not found";

    public static string UnaryOperatorTypeMismatch(string op, TypeSymbol operand) =>
        $"Operator `{op}` cannot be applied to operand of type `{operand.Name}`";

    public static string BinaryOperatorTypeMismatch(string op, TypeSymbol left, TypeSymbol right) =>
        $"Operator `{op}` cannot be applied to operands of type `{left.Name}` and `{right.Name}`";

    public const string ExpressionIsNotAssignable = "Expression is not assignable";

    public static string SymbolIsNotAssignable(string name) => $"`{name}` is not assignable";

    public static string NameIsImmutable(string name) => $"`{name}` is immutable";

    public const string AllCodePathsMustReturn = "Not all code paths return a value";

    public static string NameIsAlreadyDefined(Symbol symbol) => $"`{symbol.Name}` is already defined";

    public const string ExpressionIsNotCallable = "Expression is not callable";

    public static string ValueOfTypeIsNotCallable(TypeSymbol type) => $"Value of type `{type.Name}` is not callable";

    public static string FunctionOnlyExpectsNArguments(FunctionSymbol function) =>
        $"`{function.Name}` only expects {function.Parameters.Length} argument(s)";

    public const string FunctionMustHaveABody = "Non-extern functions must have a body";
    public const string FunctionMustNotHaveABody = "Extern functions must not have a body";

    public static string NameIsNotAType(string name) => $"`{name}` is not a type";

    public static string TypeDoesNotContainMember(TypeSymbol type, string fieldName) =>
        $"`{type.Name}` does not contain member `{fieldName}`";

    public static string CycleDetected(FieldSymbol field) =>
        $"Cycle detected. The type `{field.Type.Name}` of field `{field.Name}` references `{field.ContainingType.Name}`";

    public const string CodeIsUnreachable = "Code is unreachable";

    public static string ValueDoesNotFitInType(TypeSymbol type) => $"Value does not fit in type `{type.Name}`";
}
