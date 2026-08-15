namespace ILToCSConverter.Core.Conversion;

public sealed class FileConversionResult
{
    public required string SourcePath { get; init; }
    public string? OutputFolder { get; init; }
    public string? ProjectFile { get; init; }
    public ConversionStatus Status { get; init; }
    public string? Message { get; init; }
    public string? Detail { get; init; }
    public int GeneratedFileCount { get; init; }
    public bool BuildSucceeded { get; init; }
    public string? Sha256 { get; init; }
    public long SourceLength { get; init; }
}

public sealed record ConversionBatchResult
{
    public required IReadOnlyList<FileConversionResult> Files { get; init; }
    public required string OutputPath { get; init; }
    public string? ReportPath { get; init; }
    public string? JUnitPath { get; init; }
    public string? SolutionPath { get; init; }
    public TimeSpan Duration { get; init; }

    public int Total => Files.Count;
    public int Succeeded => Files.Count(f => f.Status == ConversionStatus.Succeeded);
    public int Failed => Files.Count(f => f.Status == ConversionStatus.Failed);
    public int Skipped => Files.Count(f => f.Status == ConversionStatus.Skipped);
}
