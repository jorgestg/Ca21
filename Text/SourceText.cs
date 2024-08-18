namespace Ca21.Text;

public sealed class SourceText(string fileName, ReadOnlyMemory<char> text)
{
    public string FileName { get; } = fileName;
    public ReadOnlyMemory<char> Text { get; } = text;
}
