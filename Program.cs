using Antlr4.Runtime;
using Ca21;
using Ca21.Antlr;
using Ca21.CodeGen;
using Ca21.Symbols;
using Ca21.Text;

const string source = """
    extern("js print") func print(n int32);

    struct Point { x int32, y int32 }
    
    func main() {
        let p = Point { x = 4, y = 5 };
        print(p.x);
    }
    """;

var sourceText = new SourceText("main.ca21", source.AsMemory());
var charStream = CharStreams.fromString(source);
SourceTextMap.Register(charStream, sourceText);

var parser = new Ca21Parser(new CommonTokenStream(new Ca21Lexer(charStream)));
var compilationUnit = parser.compilationUnit();
var module = new ModuleSymbol(compilationUnit, "main");
var compiler = Compiler.Compile(module);
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
    var writer = new StringWriter();
    WatEmitter.Emit(compiler.ModuleSymbol, compiler.Bodies, writer);
    Console.WriteLine(writer.ToString());
}
