namespace ILToCSConverter.Core.Conversion;

public static class FileDiscovery
{
    public static readonly string[] SupportedExtensions = [".il", ".dll", ".exe", ".netmodule"];

    public static IReadOnlyList<string> Discover(
        string inputPath,
        string outputPath,
        bool recursive,
        IConversionLogger logger,
        IReadOnlyList<string>? extensions = null)
    {
        var allowed = extensions ?? SupportedExtensions;

        if (File.Exists(inputPath))
        {
            if (!IsSupported(inputPath, allowed))
            {
                logger.Warn($"Desteklenmeyen dosya türü: {inputPath}");
                return [];
            }

            return [Path.GetFullPath(inputPath)];
        }

        if (!Directory.Exists(inputPath))
            throw new DirectoryNotFoundException($"Kaynak bulunamadı: {inputPath}");

        string fullInput = Path.GetFullPath(inputPath);
        string fullOutput = Path.GetFullPath(outputPath);
        var option = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;

        var files = Directory.EnumerateFiles(fullInput, "*.*", option)
            .Where(path => IsSupported(path, allowed))
            .Where(path => !IsInside(path, fullOutput))
            .Where(path => !path.EndsWith(".resources.dll", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return files;
    }

    public static bool IsSupported(string path) => IsSupported(path, SupportedExtensions);

    public static bool IsSupported(string path, IReadOnlyList<string> extensions)
    {
        string ext = Path.GetExtension(path);
        return extensions.Any(e => e.Equals(ext, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsInside(string path, string folder)
    {
        string fullPath = Path.GetFullPath(path);
        string fullFolder = Path.GetFullPath(folder).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        return fullPath.StartsWith(fullFolder, StringComparison.OrdinalIgnoreCase);
    }
}
