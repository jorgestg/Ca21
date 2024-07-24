namespace Ca21.Binding;

public readonly struct BoundConstant(object value)
{
    public bool HasValue { get; } = true;
    public object Value { get; } = value;
}
