using System.Text.Json;
using System.Text.Json.Serialization;
using ILToCSConverter.Core.Conversion;

namespace ILToCSConverter.Core.Settings;

public sealed class AppSettings
{
    public string? IlasmPath { get; set; }
    public string? IldasmPath { get; set; }
    public string LanguageVersion { get; set; } = "CSharp12";
    public bool Recursive { get; set; } = true;
    public int MaxParallelism { get; set; }
    public bool VerifyBuild { get; set; }
    public bool FormatOutput { get; set; }
    public bool IncludeGlobalNamespace { get; set; } = true;
    public bool IncludeCompilerGenerated { get; set; }
    public bool IncludeNestedTypes { get; set; } = true;
    public bool GenerateSolution { get; set; } = true;
    public bool DecompileBaml { get; set; } = true;
    public bool UsePdb { get; set; } = true;
    public bool DarkTheme { get; set; }
    public string UiLanguage { get; set; } = "tr";
    public string CollisionStrategy { get; set; } = nameof(Conversion.CollisionStrategy.SubfolderThenHash);
    public List<string> RecentInputs { get; set; } = [];
    public List<string> RecentOutputs { get; set; } = [];
    public List<string> RecentDuzunInputs { get; set; } = [];
    public List<string> RecentDuzunOutputs { get; set; } = [];
    public string? LastInput { get; set; }
    public string? LastOutput { get; set; }
    public string? LastDuzunInput { get; set; }
    public string? LastDuzunOutput { get; set; }
    public string? LastSnkPath { get; set; }
    public string? LastTokenMap { get; set; }
    public string? LastPackInput { get; set; }
    public string? LastPackOutput { get; set; }
    public bool GenerateSnkIfMissing { get; set; } = true;
    public bool StripSignature { get; set; }
    public bool ReplaceAllExternTokens { get; set; }
    public List<string> RecentPackInputs { get; set; } = [];
    public List<string> RecentPackOutputs { get; set; } = [];

    public ConversionOptions ToOptions(string inputPath, string outputPath) => new()
    {
        InputPath = inputPath,
        OutputPath = outputPath,
        Recursive = Recursive,
        MaxParallelism = MaxParallelism,
        IlasmPath = string.IsNullOrWhiteSpace(IlasmPath) ? null : IlasmPath,
        LanguageVersion = LanguageVersion,
        VerifyBuild = VerifyBuild,
        FormatOutput = FormatOutput,
        IncludeGlobalNamespace = IncludeGlobalNamespace,
        IncludeCompilerGenerated = IncludeCompilerGenerated,
        IncludeNestedTypes = IncludeNestedTypes,
        GenerateSolution = GenerateSolution,
        DecompileBaml = DecompileBaml,
        UsePdb = UsePdb
    };
}

public static class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public static string UserSettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ILToCSConverter",
        "settings.json");

    public static AppSettings Load()
    {
        var settings = new AppSettings();
        MergeFile(settings, Path.Combine(AppContext.BaseDirectory, "appsettings.json"));
        MergeFile(settings, UserSettingsPath);
        return settings;
    }

    public static void Save(AppSettings settings)
    {
        string path = UserSettingsPath;
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, JsonSerializer.Serialize(settings, JsonOptions));
    }

    public static void RememberPath(List<string> recent, string path, int limit = 8)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        string full = Path.GetFullPath(path);
        recent.RemoveAll(item => string.Equals(item, full, StringComparison.OrdinalIgnoreCase));
        recent.Insert(0, full);
        if (recent.Count > limit)
            recent.RemoveRange(limit, recent.Count - limit);
    }

    private static void MergeFile(AppSettings target, string path)
    {
        if (!File.Exists(path))
            return;

        try
        {
            var loaded = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(path), JsonOptions);
            if (loaded is null)
                return;

            if (!string.IsNullOrWhiteSpace(loaded.IlasmPath))
                target.IlasmPath = loaded.IlasmPath;
            if (!string.IsNullOrWhiteSpace(loaded.IldasmPath))
                target.IldasmPath = loaded.IldasmPath;
            if (!string.IsNullOrWhiteSpace(loaded.LanguageVersion))
                target.LanguageVersion = loaded.LanguageVersion;
            target.Recursive = loaded.Recursive;
            target.MaxParallelism = loaded.MaxParallelism;
            target.VerifyBuild = loaded.VerifyBuild;
            target.FormatOutput = loaded.FormatOutput;
            target.IncludeGlobalNamespace = loaded.IncludeGlobalNamespace;
            target.IncludeCompilerGenerated = loaded.IncludeCompilerGenerated;
            target.IncludeNestedTypes = loaded.IncludeNestedTypes;
            target.GenerateSolution = loaded.GenerateSolution;
            target.DecompileBaml = loaded.DecompileBaml;
            target.UsePdb = loaded.UsePdb;
            target.DarkTheme = loaded.DarkTheme;
            if (!string.IsNullOrWhiteSpace(loaded.UiLanguage))
                target.UiLanguage = loaded.UiLanguage;
            if (!string.IsNullOrWhiteSpace(loaded.CollisionStrategy))
                target.CollisionStrategy = loaded.CollisionStrategy;
            if (loaded.RecentInputs.Count > 0)
                target.RecentInputs = loaded.RecentInputs;
            if (loaded.RecentOutputs.Count > 0)
                target.RecentOutputs = loaded.RecentOutputs;
            if (loaded.RecentDuzunInputs.Count > 0)
                target.RecentDuzunInputs = loaded.RecentDuzunInputs;
            if (loaded.RecentDuzunOutputs.Count > 0)
                target.RecentDuzunOutputs = loaded.RecentDuzunOutputs;
            if (!string.IsNullOrWhiteSpace(loaded.LastInput))
                target.LastInput = loaded.LastInput;
            if (!string.IsNullOrWhiteSpace(loaded.LastOutput))
                target.LastOutput = loaded.LastOutput;
            if (!string.IsNullOrWhiteSpace(loaded.LastDuzunInput))
                target.LastDuzunInput = loaded.LastDuzunInput;
            if (!string.IsNullOrWhiteSpace(loaded.LastDuzunOutput))
                target.LastDuzunOutput = loaded.LastDuzunOutput;
            if (!string.IsNullOrWhiteSpace(loaded.LastSnkPath))
                target.LastSnkPath = loaded.LastSnkPath;
            if (!string.IsNullOrWhiteSpace(loaded.LastTokenMap))
                target.LastTokenMap = loaded.LastTokenMap;
            if (!string.IsNullOrWhiteSpace(loaded.LastPackInput))
                target.LastPackInput = loaded.LastPackInput;
            if (!string.IsNullOrWhiteSpace(loaded.LastPackOutput))
                target.LastPackOutput = loaded.LastPackOutput;
            target.GenerateSnkIfMissing = loaded.GenerateSnkIfMissing;
            target.StripSignature = loaded.StripSignature;
            target.ReplaceAllExternTokens = loaded.ReplaceAllExternTokens;
            if (loaded.RecentPackInputs.Count > 0)
                target.RecentPackInputs = loaded.RecentPackInputs;
            if (loaded.RecentPackOutputs.Count > 0)
                target.RecentPackOutputs = loaded.RecentPackOutputs;
        }
        catch
        {
            // Bozuk ayar dosyası uygulamayı durdurmasın.
        }
    }
}
