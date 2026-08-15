namespace ILToCSConverter.Core.Conversion;

public sealed record ConversionProgress(
    int Completed,
    int Total,
    string CurrentFile,
    string Message,
    ConversionStatus Status);

public enum ConversionStatus
{
    Queued,
    Running,
    Succeeded,
    Failed,
    Skipped,
    Cancelled
}
