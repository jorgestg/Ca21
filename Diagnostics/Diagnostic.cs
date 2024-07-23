using Ca21.Sources;

namespace Ca21.Diagnostics;

public record struct Diagnostic(SourceSpan Position, string Message);
