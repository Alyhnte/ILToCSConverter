namespace ILToCSConverter.Core.Signing;

public sealed class AlignmentResult
{
    public required string UpdatedIl { get; init; }
    public string? OldToken { get; init; }
    public required string NewToken { get; init; }
    public bool HasPublicKey { get; init; }
    public int UpdatedTokenCount { get; init; }
}
