using Antlr4.Runtime;
using Ca21;
using Ca21.Antlr;
using Ca21.Sources;
using Ca21.Symbols;

const string source = """
    func fib() int32 {
        let mut a = 0;
        let mut b = 1;
        let mut c = a + b;
        while c <= 100 {
            a = b;
            b = c;
            c = a + b;
        }

        return c;
    }
    """;

var sourceText = new SourceText("main.ca21", source.AsMemory());
var charStream = CharStreams.fromString(source);
SourceTextMap.Register(charStream, sourceText);

var parser = new Ca21Parser(new CommonTokenStream(new Ca21Lexer(charStream)));
var compilationUnit = parser.compilationUnit();
var main = new SourceFunctionSymbol(compilationUnit.Function);
var compiler = Compiler.Compile(main);
if (compiler.Diagnostics.Any())
{
    Console.ForegroundColor = ConsoleColor.DarkRed;
    foreach (var diagnostic in compiler.Diagnostics)
    {
        Console.WriteLine(
            "{0}({1}, {2}): {3}",
            diagnostic.Position.Source.FileName,
            diagnostic.Position.GetLine(),
            diagnostic.Position.GetColumn(),
            diagnostic.Message
        );
    }

    Console.ResetColor();
}
else
{
    Console.WriteLine(compiler.ToString());
}
