namespace Ca21.Sources;

public sealed class SourceText(string fileName, ReadOnlyMemory<char> text)
{
    public string FileName { get; } = fileName;
    public ReadOnlyMemory<char> Text { get; } = text;
}
