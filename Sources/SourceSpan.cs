namespace Ca21.Sources;

public readonly struct SourceSpan(SourceText source, int start, int length)
{
    public SourceText Source { get; } = source;

    public int Start { get; } = start;
    public int Length { get; } = length;
    public int End => Start + End;

    public ReadOnlySpan<char> GetText()
    {
        return Source.Text.Span.Slice(Start, Length);
    }

    public int GetLine()
    {
        var lineNumber = 0;
        var position = 0;
        foreach (var line in Source.Text.Span.EnumerateLines())
        {
            lineNumber++;
            position += line.Length;
            if (position >= Start)
                break;
        }

        return lineNumber;
    }

    public int GetColumn()
    {
        var source = Source.Text.Span;
        var startOfLine = -1;
        for (var i = Start; i >= 0; i--)
        {
            var c = source[i];
            if (c == '\n')
            {
                startOfLine = i;
                break;
            }
        }

        return Start - startOfLine;
    }
}
