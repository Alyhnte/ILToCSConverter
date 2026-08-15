using System.Text.RegularExpressions;

namespace ILToCSConverter.Core.Signing;

public sealed partial class StrongNameAligner
{
    [GeneratedRegex(@"((?:^|\r?\n)\.assembly\s+(?!extern\b)(?:'[^']+'|[\w\.-]+)\s*\{[\s\S]*?\.publickey\s*=\s*\()([\s\S]*?)(\))", RegexOptions.IgnoreCase)]
    private static partial Regex ExistingAssemblyPublicKeyRegex();

    [GeneratedRegex(@"((?:^|\r?\n)\.assembly\s+(?!extern\b)(?:'[^']+'|[\w\.-]+)\s*\{)", RegexOptions.IgnoreCase)]
    private static partial Regex AssemblyHeaderRegex();

    [GeneratedRegex(@"\.publickeytoken\s*=\s*\(([\s\S]*?)\)", RegexOptions.IgnoreCase)]
    private static partial Regex PublicKeyTokenRegex();

    public static AlignmentResult Align(string ilContent, StrongNameKey newKey)
    {
        ArgumentNullException.ThrowIfNull(ilContent);
        ArgumentNullException.ThrowIfNull(newKey);

        string? oldToken = null;
        bool hasExistingPublicKey = false;

        string updatedIl = ExistingAssemblyPublicKeyRegex().Replace(ilContent, match =>
        {
            hasExistingPublicKey = true;
            oldToken = StrongNameKey.ComputeTokenFromIlHex(match.Groups[2].Value);
            return $"{match.Groups[1].Value}{newKey.FormattedPublicKeyHex}{match.Groups[3].Value}";
        }, 1);

        if (!hasExistingPublicKey)
        {
            updatedIl = AssemblyHeaderRegex().Replace(updatedIl, match =>
                $"{match.Groups[1].Value}{Environment.NewLine}  .publickey = ({newKey.FormattedPublicKeyHex})", 1);

            return new AlignmentResult
            {
                UpdatedIl = updatedIl,
                OldToken = null,
                NewToken = newKey.Token,
                HasPublicKey = AssemblyHeaderRegex().IsMatch(updatedIl),
                UpdatedTokenCount = 0
            };
        }

        int replacedTokenCount = 0;
        if (!string.IsNullOrEmpty(oldToken) &&
            !oldToken.Equals(newKey.Token, StringComparison.OrdinalIgnoreCase))
        {
            string normalizedOldToken = StrongNameKey.NormalizeHex(oldToken);
            updatedIl = PublicKeyTokenRegex().Replace(updatedIl, match =>
            {
                string currentTokenHex = StrongNameKey.NormalizeHex(match.Groups[1].Value);
                if (!string.Equals(currentTokenHex, normalizedOldToken, StringComparison.OrdinalIgnoreCase))
                    return match.Value;

                replacedTokenCount++;
                return $".publickeytoken = ({newKey.FormattedTokenHex})";
            });
        }

        return new AlignmentResult
        {
            UpdatedIl = updatedIl,
            OldToken = oldToken,
            NewToken = newKey.Token,
            HasPublicKey = true,
            UpdatedTokenCount = replacedTokenCount
        };
    }

    public static AlignmentResult AlignFile(string ilFilePath, StrongNameKey newKey, string? outputFilePath = null)
    {
        string content = File.ReadAllText(ilFilePath);
        var result = Align(content, newKey);
        File.WriteAllText(outputFilePath ?? ilFilePath, result.UpdatedIl);
        return result;
    }
}

public sealed class AlignmentResult
{
    public required string UpdatedIl { get; init; }
    public string? OldToken { get; init; }
    public required string NewToken { get; init; }
    public bool HasPublicKey { get; init; }
    public int UpdatedTokenCount { get; init; }
}
