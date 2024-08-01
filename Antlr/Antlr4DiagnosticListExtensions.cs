using Antlr4.Runtime;
using Ca21.Antlr;
using Ca21.Sources;

namespace Ca21.Diagnostics;

internal static class Antlr4DiagnosticListExtensions
{
    public static void Add(this DiagnosticList diagnostics, ParserRuleContext context, string message)
    {
        var sourceText = SourceTextMap.Retrieve(context.Start.InputStream);
        var sourceSpan = new SourceSpan(sourceText, context.Start.StartIndex, context.Stop.StopIndex);
        diagnostics.Add(new Diagnostic(sourceSpan, message));
    }

    public static void Add(this DiagnosticList diagnostics, IToken token, string message)
    {
        var sourceText = SourceTextMap.Retrieve(token.InputStream);
        var sourceSpan = new SourceSpan(sourceText, token.StartIndex, token.StopIndex);
        diagnostics.Add(new Diagnostic(sourceSpan, message));
    }
}
