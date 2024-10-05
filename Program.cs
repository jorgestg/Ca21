using System.CommandLine;
using System.Diagnostics;
using Antlr4.Runtime;
using Ca21;
using Ca21.Antlr;
using Ca21.Backends;
using Ca21.Symbols;
using Ca21.Text;
using static Ca21.Antlr.Ca21Parser;

var rootCommand = new RootCommand("Ca21 Compiler CLI");

var filesArgument = new Argument<IList<string>>("file", "The source file(s) to compile")
{
    Arity = ArgumentArity.OneOrMore
};

rootCommand.AddArgument(filesArgument);

var outOption = new Option<string?>("--out", "The output filename");
outOption.AddAlias("-o");
rootCommand.AddOption(outOption);

var transpileOnlyOption = new Option<bool>("--transpile-only", "Just output C code");
rootCommand.AddOption(transpileOnlyOption);

rootCommand.SetHandler(Compile, filesArgument, outOption, transpileOnlyOption);
rootCommand.Invoke(args);

static void Compile(IList<string> files, string? outputPath, bool transpileOnly)
{
    try
    {
        var rootsBuilder = new ArrayBuilder<CompilationUnitContext>(files.Count);
        foreach (var file in files)
        {
            var source = file == "-" ? Console.In.ReadToEnd() : File.ReadAllText(file);
            var sourceText = new SourceText(file == "-" ? "stdin" : file, source.AsMemory());
            var charStream = CharStreams.fromString(source);
            SourceTextMap.Register(charStream, sourceText);

            var parser = new Ca21Parser(new CommonTokenStream(new Ca21Lexer(charStream)));
            var compilationUnit = parser.compilationUnit();
            if (parser.NumberOfSyntaxErrors > 0)
                continue;

            rootsBuilder.Add(compilationUnit);
        }

        if (files.Count == 0)
            return;

        var module = new ModuleSymbol(rootsBuilder.MoveToImmutable(), "main");
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
            return;
        }

        TextWriter writer;
        if (transpileOnly)
        {
            writer = outputPath == null ? Console.Out : new StreamWriter(outputPath);
            C99Backend.Emit(compiler, writer);
            return;
        }

        writer = new StringWriter();
        C99Backend.Emit(compiler, writer);
        using var process = Process.Start(
            new ProcessStartInfo
            {
                FileName = "cc",
                Arguments = $"-x c -o {outputPath ?? Path.GetFileNameWithoutExtension(files.First())} -",
                RedirectStandardInput = true,
                UseShellExecute = false
            }
        );

        if (process == null)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.Error.WriteLine("Failed to start cc");
            Console.ResetColor();
            return;
        }

        process.StandardInput.Write(writer.ToString());
        process.StandardInput.Flush();
    }
    catch (IOException e)
    {
        Console.ForegroundColor = ConsoleColor.DarkRed;
        Console.Error.WriteLine(e.Message);
    }
}
