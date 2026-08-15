using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.Metadata;
using ILToCSConverter.Core.Conversion;
using ILToCSConverter.Core.DebugSymbols;

namespace ILToCSConverter.Core.Decompile;

public sealed class ProjectDecompilerHost
{
    public int DecompileAssembly(
        string assemblyPath,
        string outputFolder,
        ConversionOptions options,
        int maxDegreeOfParallelism,
        IConversionLogger logger,
        CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(outputFolder);

        using var module = new PEFile(assemblyPath);
        string targetFramework = module.DetectTargetFrameworkId();
        if (string.IsNullOrWhiteSpace(targetFramework))
            targetFramework = ".NETFramework,Version=v4.8";

        var resolver = new UniversalAssemblyResolver(
            assemblyPath,
            throwOnError: false,
            targetFramework);

        LoadedDebugSymbols? symbols = null;
        try
        {
            resolver.AddSearchDirectory(Path.GetDirectoryName(assemblyPath)!);

            if (options.UsePdb)
            {
                symbols = DebugSymbolLoader.TryLoad(module);
                if (symbols.Provider is not null)
                    logger.Info($"PDB: {symbols.Description}");
            }

            var settings = new DecompilerSettings(LanguageVersionMapper.Parse(options.LanguageVersion))
            {
                ThrowOnAssemblyResolveErrors = false,
                UseSdkStyleProjectFormat = WholeProjectDecompiler.CanUseSdkStyleProjectFormat(module),
                UseNestedDirectoriesForNamespaces = true
            };

            var decompiler = new StreamingProjectDecompiler(options, settings, resolver, symbols?.Provider)
            {
                MaxDegreeOfParallelism = Math.Max(1, maxDegreeOfParallelism)
            };

            decompiler.DecompileProject(module, outputFolder, cancellationToken);

            TargetFrameworkMapper.PatchOutputFolder(outputFolder, module);

            if (options.DecompileBaml)
            {
                int xamlCount = BamlXamlConverter.ConvertExtractedBaml(module, resolver, outputFolder, cancellationToken);
                if (xamlCount > 0)
                    logger.Info($"{xamlCount} BAML dosyası XAML'e çevrildi.");
            }

            return CountGeneratedFiles(outputFolder);
        }
        finally
        {
            symbols?.Dispose();
            (resolver as IDisposable)?.Dispose();
        }
    }

    private static int CountGeneratedFiles(string outputFolder)
    {
        if (!Directory.Exists(outputFolder))
            return 0;

        return Directory.EnumerateFiles(outputFolder, "*.*", SearchOption.AllDirectories)
            .Count(path =>
            {
                string ext = Path.GetExtension(path);
                return ext.Equals(".cs", StringComparison.OrdinalIgnoreCase)
                    || ext.Equals(".csproj", StringComparison.OrdinalIgnoreCase)
                    || ext.Equals(".resx", StringComparison.OrdinalIgnoreCase)
                    || ext.Equals(".xaml", StringComparison.OrdinalIgnoreCase)
                    || ext.Equals(".sln", StringComparison.OrdinalIgnoreCase);
            });
    }
}
