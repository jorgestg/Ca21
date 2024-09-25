using System.CommandLine;
using Antlr4.Runtime;
using Ca21;
using Ca21.Antlr;
using Ca21.Backends;
using Ca21.Symbols;
using Ca21.Text;

var rootCommand = new RootCommand("Ca21 Compiler CLI");

var fileNameArgument = new Argument<string>("fileName", "The source file to compile");
rootCommand.AddArgument(fileNameArgument);

var outOption = new Option<string?>("--out", "The output filename");
outOption.AddAlias("-o");
rootCommand.AddOption(outOption);

rootCommand.SetHandler(Compile, fileNameArgument, outOption);
rootCommand.Invoke(args);

static void Compile(string fileName, string? outputPath)
{
    try
    {
        var source = File.ReadAllText(fileName);
        var sourceText = new SourceText(fileName, source.AsMemory());
        var charStream = CharStreams.fromString(source);
        SourceTextMap.Register(charStream, sourceText);

        var parser = new Ca21Parser(new CommonTokenStream(new Ca21Lexer(charStream)));
        var compilationUnit = parser.compilationUnit();
        var module = new ModuleSymbol(compilationUnit, "main");
        var compiler = Compiler.Compile(module);
        if (!compiler.Diagnostics.Any())
        {
            using TextWriter writer = outputPath == null ? Console.Out : new StreamWriter(outputPath);
            C99Backend.Emit(compiler, writer);
            return;
        }

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
    catch (IOException e)
    {
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.Error.WriteLine(e.Message);
        return;
    }
}
