using System.CommandLine;
using System.Diagnostics;
using Ca21;
using Ca21.Backends;
using Ca21.Symbols;

var rootCommand = new RootCommand("Ca21 Compiler CLI");

var directoryArgument = new Argument<string>("directory", "The directory to compile");
rootCommand.AddArgument(directoryArgument);

var packageNameOption = new Option<string>("--package", "The package name");
packageNameOption.AddAlias("-pkg");
rootCommand.AddOption(packageNameOption);

var outOption = new Option<string?>("--out", "The output filename");
outOption.AddAlias("-o");
rootCommand.AddOption(outOption);

var transpileOnlyOption = new Option<bool>("--transpile-only", "Just output C code");
rootCommand.AddOption(transpileOnlyOption);

rootCommand.SetHandler(Compile, directoryArgument, packageNameOption, outOption, transpileOnlyOption);
rootCommand.Invoke(args);

static void Compile(string directory, string? packageName, string? outputPath, bool transpileOnly)
{
    var directoryInfo = new DirectoryInfo(directory);
    if (!directoryInfo.Exists)
    {
        WriteError($"Directory `{directory}` does not exist");
        return;
    }

    try
    {
        var package = PackageSymbol.FromDirectory(directoryInfo, packageName);
        if (package.Modules.Length == 0)
            return;

        var compiler = Compiler.Compile(package);
        if (compiler.Diagnostics.Any())
        {
            foreach (var diagnostic in compiler.Diagnostics)
            {
                WriteError(
                    string.Format(
                        "{0}({1}, {2}): {3}",
                        diagnostic.Position.Source.FileName,
                        diagnostic.Position.GetLine(),
                        diagnostic.Position.GetColumn(),
                        diagnostic.Message
                    )
                );
            }

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
                Arguments = $"-x c -o {outputPath ?? directoryInfo.Name} -",
                RedirectStandardInput = true,
                UseShellExecute = false
            }
        );

        if (process == null)
        {
            WriteError("Failed to start `cc` process");
            return;
        }

        process.StandardInput.Write(writer.ToString());
        process.StandardInput.Flush();
    }
    catch (IOException e)
    {
        WriteError(e.Message);
    }
}

static void WriteError(string message)
{
    Console.ForegroundColor = ConsoleColor.DarkRed;
    Console.Error.WriteLine(message);
}
