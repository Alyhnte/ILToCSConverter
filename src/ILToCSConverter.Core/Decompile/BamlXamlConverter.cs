using ICSharpCode.BamlDecompiler;
using ICSharpCode.Decompiler.Metadata;

namespace ILToCSConverter.Core.Decompile;

public static class BamlXamlConverter
{
    public static int ConvertExtractedBaml(
        MetadataFile module,
        IAssemblyResolver resolver,
        string outputFolder,
        CancellationToken cancellationToken)
    {
        if (!Directory.Exists(outputFolder))
            return 0;

        var bamlFiles = Directory.GetFiles(outputFolder, "*.baml", SearchOption.AllDirectories);
        if (bamlFiles.Length == 0)
            return 0;

        var settings = new BamlDecompilerSettings { ThrowOnAssemblyResolveErrors = false };
        var typeSystem = new BamlDecompilerTypeSystem(module, resolver);
        var decompiler = new XamlDecompiler(typeSystem, settings)
        {
            CancellationToken = cancellationToken
        };

        int converted = 0;
        foreach (string bamlPath in bamlFiles)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                using var stream = File.OpenRead(bamlPath);
                var result = decompiler.Decompile(stream);
                string xamlPath = Path.ChangeExtension(bamlPath, ".xaml");
                result.Xaml.Save(xamlPath);
                File.Delete(bamlPath);
                ReplaceInProjects(outputFolder, Path.GetFileName(bamlPath), Path.GetFileName(xamlPath));
                converted++;
            }
            catch
            {
                // Tek BAML bozulması tüm dönüşümü düşürmesin.
            }
        }

        return converted;
    }

    private static void ReplaceInProjects(string outputFolder, string fromFile, string toFile)
    {
        foreach (string csproj in Directory.EnumerateFiles(outputFolder, "*.csproj", SearchOption.AllDirectories))
        {
            string text = File.ReadAllText(csproj);
            if (!text.Contains(fromFile, StringComparison.OrdinalIgnoreCase))
                continue;
            File.WriteAllText(csproj, text.Replace(fromFile, toFile, StringComparison.OrdinalIgnoreCase));
        }
    }
}
