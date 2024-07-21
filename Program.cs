using Antlr4.Runtime;
using Ca21;
using Ca21.Antlr;
using Ca21.Symbols;

const string source = """
    func main() int32 {
        let mut n = 1;
        while n < 100 {
            n = n * 2;
            return n;
        }
    }
    """;

var parser = new Ca21Parser(new CommonTokenStream(new Ca21Lexer(CharStreams.fromString(source))));
var compilationUnit = parser.compilationUnit();
var main = new SourceFunctionSymbol(compilationUnit.Function);
var compiler = Compiler.Compile(main);
if (compiler.Diagnostics.Any())
{
    foreach (var diagnostic in compiler.Diagnostics)
        Console.WriteLine(diagnostic.Message);
}
else
{
    Console.WriteLine(compiler.ToString());
}
