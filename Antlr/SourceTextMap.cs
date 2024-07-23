using Antlr4.Runtime;
using Ca21.Sources;

namespace Ca21.Antlr;

internal static class SourceTextMap
{
    private static readonly Dictionary<ICharStream, SourceText> Map = new();

    public static void Register(ICharStream charStream, SourceText sourceText) => Map[charStream] = sourceText;

    public static SourceText Retrieve(ICharStream charStream) => Map[charStream];
}
