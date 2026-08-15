namespace ILToCSConverter.Core.Conversion;

public enum CollisionStrategy
{
    SubfolderThenHash
}

public sealed class ConversionOptions
{
    public required string InputPath { get; init; }
    public required string OutputPath { get; init; }
    public bool Recursive { get; init; } = true;
    public int MaxParallelism { get; init; }
    public string? IlasmPath { get; init; }
    public string LanguageVersion { get; init; } = "CSharp12";
    public bool VerifyBuild { get; init; }
    public bool FormatOutput { get; init; }
    public bool IncludeGlobalNamespace { get; init; } = true;
    public bool IncludeCompilerGenerated { get; init; }
    public bool IncludeNestedTypes { get; init; } = true;
    public CollisionStrategy CollisionStrategy { get; init; } = CollisionStrategy.SubfolderThenHash;
    public string? ReportPath { get; init; }
    public bool GenerateSolution { get; init; } = true;
    public bool WriteJUnitReport { get; init; } = true;
    public bool DecompileBaml { get; init; } = true;
    public bool UsePdb { get; init; } = true;

    public int EffectiveMaxParallelism =>
        MaxParallelism > 0 ? MaxParallelism : Math.Max(1, Environment.ProcessorCount);
}
