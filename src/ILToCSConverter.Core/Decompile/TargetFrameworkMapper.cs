using System.Text.RegularExpressions;
using ICSharpCode.Decompiler.Metadata;

namespace ILToCSConverter.Core.Decompile;

public static class TargetFrameworkMapper
{
    public static string ToSdkMoniker(string? targetFrameworkId)
    {
        if (string.IsNullOrWhiteSpace(targetFrameworkId))
            return "net48";

        string id = targetFrameworkId.Trim();
        string? version = ExtractVersion(id);

        if (id.StartsWith(".NETCoreApp", StringComparison.OrdinalIgnoreCase) ||
            id.StartsWith("Microsoft.NETCore.App", StringComparison.OrdinalIgnoreCase))
            return "net" + NormalizeCore(version ?? "8.0");

        if (id.StartsWith(".NETStandard", StringComparison.OrdinalIgnoreCase))
            return "netstandard" + NormalizeCore(version ?? "2.0");

        if (id.StartsWith(".NETFramework", StringComparison.OrdinalIgnoreCase) ||
            id.Contains("Version=v4", StringComparison.OrdinalIgnoreCase))
            return "net" + NormalizeFramework(version ?? "4.8");

        return "net48";
    }

    public static string FromModule(MetadataFile module)
    {
        string? id = module.DetectTargetFrameworkId();
        return ToSdkMoniker(id);
    }

    public static void PatchProject(string csprojPath, string moniker)
    {
        if (!File.Exists(csprojPath))
            return;

        string text = File.ReadAllText(csprojPath);
        string updated = Regex.Replace(
            text,
            @"<TargetFrameworks?>.*?</TargetFrameworks?>",
            $"<TargetFramework>{moniker}</TargetFramework>",
            RegexOptions.IgnoreCase | RegexOptions.Singleline);

        if (!updated.Equals(text, StringComparison.Ordinal))
            File.WriteAllText(csprojPath, updated);
        else if (!text.Contains("<TargetFramework", StringComparison.OrdinalIgnoreCase))
        {
            updated = text.Replace("</PropertyGroup>", $"  <TargetFramework>{moniker}</TargetFramework>{Environment.NewLine}  </PropertyGroup>", StringComparison.Ordinal);
            File.WriteAllText(csprojPath, updated);
        }
    }

    public static void PatchOutputFolder(string outputFolder, MetadataFile module)
    {
        string moniker = FromModule(module);
        foreach (string csproj in Directory.EnumerateFiles(outputFolder, "*.csproj", SearchOption.AllDirectories))
            PatchProject(csproj, moniker);
    }

    private static string? ExtractVersion(string id)
    {
        int index = id.IndexOf("Version=", StringComparison.OrdinalIgnoreCase);
        if (index < 0)
            return null;
        string rest = id[(index + "Version=".Length)..].TrimStart('v', 'V');
        int comma = rest.IndexOf(',');
        if (comma >= 0)
            rest = rest[..comma];
        return rest.Trim();
    }

    private static string NormalizeCore(string version)
    {
        var parts = version.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 1)
            return parts[0] + ".0";
        return parts[0] + "." + parts[1];
    }

    private static string NormalizeFramework(string version)
    {
        string digits = version.Replace(".", "", StringComparison.Ordinal);
        if (digits.Length >= 2)
            return digits;
        return "48";
    }
}
