using Ca21.Text;

namespace Ca21.Diagnostics;

public record struct Diagnostic(SourceSpan Position, string Message);
