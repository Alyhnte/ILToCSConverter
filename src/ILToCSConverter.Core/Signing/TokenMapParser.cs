using System.Text;

namespace ILToCSConverter.Core.Signing;

public static class TokenMapParser
{
    public static Dictionary<string, string>? Parse(string? text) =>
        Parse(text is null ? [] : [text]);

    public static Dictionary<string, string>? Parse(IEnumerable<string?>? values)
    {
        if (values is null)
            return null;

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (string? value in values)
        {
            if (string.IsNullOrWhiteSpace(value))
                continue;

            foreach (string line in SplitEntries(value))
            {
                if (line.Length == 0 || line[0] == '#')
                    continue;

                int separator = line.IndexOf('=');
                if (separator < 0)
                    separator = line.IndexOf(':');
                if (separator <= 0 || separator == line.Length - 1)
                    throw new FormatException($"Geçersiz token eşlemesi: '{line}'. Beklenen biçim: eski=yeni");

                string oldToken = StrongNameKey.NormalizeHex(line[..separator]);
                string newToken = StrongNameKey.NormalizeHex(line[(separator + 1)..]);

                if (oldToken.Length == 0 || oldToken.Length % 2 != 0 ||
                    newToken.Length == 0 || newToken.Length % 2 != 0)
                {
                    throw new FormatException(
                        $"Token eşlemesi yalnızca çift uzunlukta hex olmalıdır: '{line}'.");
                }

                map[oldToken] = newToken;
            }
        }

        return map.Count == 0 ? null : map;
    }

    private static IEnumerable<string> SplitEntries(string text)
    {
        var current = new StringBuilder();
        foreach (char c in text)
        {
            if (c is ',' or ';' or '\r' or '\n')
            {
                if (current.Length > 0)
                {
                    yield return current.ToString().Trim();
                    current.Clear();
                }
            }
            else
            {
                current.Append(c);
            }
        }

        if (current.Length > 0)
            yield return current.ToString().Trim();
    }
}
