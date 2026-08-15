using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace ILToCSConverter.Core.Conversion;

public sealed class OutputPathAllocator
{
    private readonly ConcurrentDictionary<string, byte> _used =
        new(StringComparer.OrdinalIgnoreCase);

    public string Allocate(string outputRoot, string sourceFile, string inputRoot)
    {
        string name = Path.GetFileNameWithoutExtension(sourceFile);
        string? sourceDir = Path.GetDirectoryName(sourceFile);
        string relative = ".";

        if (!string.IsNullOrEmpty(sourceDir) && Directory.Exists(inputRoot))
        {
            relative = Path.GetRelativePath(inputRoot, sourceDir);
        }

        string candidate = IsEmptyRelative(relative)
            ? Path.Combine(outputRoot, name)
            : Path.Combine(outputRoot, relative, name);

        candidate = Path.GetFullPath(candidate);

        if (TryClaim(candidate))
            return candidate;

        string hash = ShortHash(sourceFile);
        string hashed = IsEmptyRelative(relative)
            ? Path.Combine(outputRoot, $"{name}_{hash}")
            : Path.Combine(outputRoot, relative, $"{name}_{hash}");

        hashed = Path.GetFullPath(hashed);
        _used.TryAdd(hashed, 0);
        return hashed;
    }

    private bool TryClaim(string candidate)
    {
        if (!_used.TryAdd(candidate, 0))
            return false;

        if (Directory.Exists(candidate) && Directory.EnumerateFileSystemEntries(candidate).Any())
            return false;

        return true;
    }

    private static bool IsEmptyRelative(string relative) =>
        string.IsNullOrWhiteSpace(relative) || relative == "." || relative == "./";

    public static string ShortHash(string value)
    {
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes)[..8].ToLowerInvariant();
    }
}
