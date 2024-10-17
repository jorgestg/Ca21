using System.Collections.Frozen;
using System.Collections.Immutable;
using Antlr4.Runtime;
using Ca21.Antlr;
using Ca21.Text;
using static Ca21.Antlr.Ca21Parser;

namespace Ca21.Symbols;

internal sealed class PackageSymbol
{
    private PackageSymbol(Dictionary<string, (string Name, List<CompilationUnitContext> Roots)> pathToModuleInfoMap)
    {
        var modules = new ArrayBuilder<ModuleSymbol>(pathToModuleInfoMap.Count);
        var moduleMap = new Dictionary<string, ModuleSymbol>(pathToModuleInfoMap.Count);
        foreach (var (path, (name, roots)) in pathToModuleInfoMap)
        {
            var module = new ModuleSymbol(this, name, roots.ToImmutableArray());
            modules.Add(module);
            moduleMap[path] = module;
        }

        Modules = modules.DrainToImmutable();
        ModuleMap = moduleMap.ToFrozenDictionary();
    }

    private PackageSymbol()
    {
        Modules = [];
        ModuleMap = FrozenDictionary<string, ModuleSymbol>.Empty;
    }

    public ImmutableArray<ModuleSymbol> Modules { get; }
    public FrozenDictionary<string, ModuleSymbol> ModuleMap { get; }

    public static PackageSymbol FromDirectory(DirectoryInfo directoryInfo, string? packageName)
    {
        var hasSyntaxErrors = false;
        var pathToModuleInfoMap = new Dictionary<string, (string Name, List<CompilationUnitContext> Roots)>();
        foreach (var fileInfo in directoryInfo.EnumerateFiles("*.ca21", SearchOption.AllDirectories))
        {
            var source = File.ReadAllText(fileInfo.FullName);
            var sourceText = new SourceText(fileInfo.FullName, source.AsMemory());
            var charStream = CharStreams.fromString(source);
            SourceTextMap.Register(charStream, sourceText);

            var parser = new Ca21Parser(new CommonTokenStream(new Ca21Lexer(charStream)));
            var compilationUnit = parser.compilationUnit();
            if (parser.NumberOfSyntaxErrors > 0)
            {
                hasSyntaxErrors = true;
                continue;
            }

            var relativePath =
                fileInfo.DirectoryName == directoryInfo.FullName
                    ? "/"
                    : fileInfo.DirectoryName!.Substring(directoryInfo.FullName.Length + 1);

            if (!pathToModuleInfoMap.TryGetValue(relativePath, out var moduleInfo))
            {
                var moduleName = fileInfo.Directory!.Name;
                var roots = new List<CompilationUnitContext>();
                moduleInfo = (moduleName, roots);
                pathToModuleInfoMap[relativePath] = moduleInfo;
            }

            moduleInfo.Roots.Add(compilationUnit);
        }

        if (hasSyntaxErrors)
            return new PackageSymbol();

        return new PackageSymbol(pathToModuleInfoMap);
    }
}
