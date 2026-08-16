using System.Text.RegularExpressions;

namespace ILToCSConverter.Core.Signing;

public sealed partial class StrongNameAligner
{
    [GeneratedRegex(@"((?:^|\r?\n)\.assembly\s+(?!extern\b)(?:'[^']+'|[\w\.-]+)\s*\{[\s\S]*?\.publickey\s*=\s*\()([\s\S]*?)(\)[\s\S]*?\})", RegexOptions.Compiled)]
    private static partial Regex ExistingAssemblyPublicKeyRegex();

    [GeneratedRegex(@"((?:^|\r?\n)\.assembly\s+(?!extern\b)(?:'[^']+'|[\w\.-]+)\s*\{)", RegexOptions.Compiled)]
    private static partial Regex AssemblyHeaderRegex();

    [GeneratedRegex(@"\.publickeytoken\s*=\s*\(([\s\S]*?)\)", RegexOptions.Compiled)]
    private static partial Regex PublicKeyTokenRegex();

    [GeneratedRegex(@"\s*\.publickey\s*=\s*\([\s\S]*?\)", RegexOptions.Compiled)]
    private static partial Regex StripPublicKeyRegex();

    [GeneratedRegex(@"\s*\.publickeytoken\s*=\s*\([\s\S]*?\)", RegexOptions.Compiled)]
    private static partial Regex StripPublicKeyTokenRegex();

    /// <summary>
    /// IL dosyasındaki Strong Name imzalarını ve tüm token referanslarını tamamen kaldırır (İmzasızlaştırır).
    /// </summary>
    public static string StripStrongName(string ilContent)
    {
        string cleaned = StripPublicKeyRegex().Replace(ilContent, string.Empty);
        return StripPublicKeyTokenRegex().Replace(cleaned, string.Empty);
    }

    /// <summary>
    /// IL içeriğindeki Public Key ve ilişkili Token değerlerini günceller.
    /// </summary>
    /// <param name="ilContent">IL metin içeriği</param>
    /// <param name="newKey">Uygulanacak yeni SNK anahtarı</param>
    /// <param name="tokenMap">Opsiyonel: Bağımlı dış kütüphanelerin eski-yeni token haritası</param>
    /// <param name="replaceAllExternTokens">True ise eşleşme aramaksızın tüm extern token'larını yeni anahtara eşitler</param>
    public static AlignmentResult Align(
        string ilContent,
        StrongNameKey newKey,
        IReadOnlyDictionary<string, string>? tokenMap = null,
        bool replaceAllExternTokens = false)
    {
        ArgumentNullException.ThrowIfNull(ilContent);
        ArgumentNullException.ThrowIfNull(newKey);

        string? oldToken = null;
        bool hasExistingPublicKey = false;

        string updatedIl = ExistingAssemblyPublicKeyRegex().Replace(ilContent, match =>
        {
            hasExistingPublicKey = true;
            string oldPublicKeyHex = match.Groups[2].Value;
            oldToken = StrongNameKey.ComputeTokenFromIlHex(oldPublicKeyHex);

            string formattedNewKey = $"\r\n\t\t\t\t{newKey.FormattedPublicKeyHex}\r\n\t\t\t";
            return $"{match.Groups[1].Value}{formattedNewKey}{match.Groups[3].Value}";
        }, 1);

        if (!hasExistingPublicKey)
        {
            updatedIl = AssemblyHeaderRegex().Replace(updatedIl, match =>
            {
                hasExistingPublicKey = true;
                return $"{match.Groups[1].Value}\r\n  .publickey = (\r\n\t\t\t\t{newKey.FormattedPublicKeyHex}\r\n  )";
            }, 1);
        }

        int replacedTokenCount = 0;
        string? normalizedOldToken = oldToken is not null ? StrongNameKey.NormalizeHex(oldToken) : null;

        updatedIl = PublicKeyTokenRegex().Replace(updatedIl, match =>
        {
            string currentTokenHex = StrongNameKey.NormalizeHex(match.Groups[1].Value);

            if (tokenMap is not null && tokenMap.TryGetValue(currentTokenHex, out string? mappedToken))
            {
                replacedTokenCount++;
                byte[] mappedBytes = Convert.FromHexString(StrongNameKey.NormalizeHex(mappedToken));
                return $".publickeytoken = ({StrongNameKey.FormatHex(mappedBytes, 8)})";
            }

            if (replaceAllExternTokens)
            {
                replacedTokenCount++;
                return $".publickeytoken = ({newKey.FormattedTokenHex})";
            }

            if (normalizedOldToken is not null && string.Equals(currentTokenHex, normalizedOldToken, StringComparison.OrdinalIgnoreCase))
            {
                replacedTokenCount++;
                return $".publickeytoken = ({newKey.FormattedTokenHex})";
            }

            return match.Value;
        });

        return new AlignmentResult
        {
            UpdatedIl = updatedIl,
            OldToken = oldToken,
            NewToken = newKey.Token,
            HasPublicKey = hasExistingPublicKey,
            UpdatedTokenCount = replacedTokenCount
        };
    }

    public static AlignmentResult AlignFile(
        string ilFilePath,
        StrongNameKey newKey,
        string? outputFilePath = null,
        IReadOnlyDictionary<string, string>? tokenMap = null,
        bool replaceAllExternTokens = false)
    {
        string content = File.ReadAllText(ilFilePath);
        var result = Align(content, newKey, tokenMap, replaceAllExternTokens);

        string targetPath = outputFilePath ?? ilFilePath;
        File.WriteAllText(targetPath, result.UpdatedIl);

        return result;
    }
}
