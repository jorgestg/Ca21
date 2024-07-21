using Antlr4.Runtime.Misc;

namespace Ca21.Diagnostics;

public record struct Diagnostic(Interval Position, string Message);
