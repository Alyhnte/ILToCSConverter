using System.Text;

namespace ILToCSConverter.Core.Plugins;

public sealed class Utf8InputPreprocessor : IInputPreprocessor
{
    public string Name => "UTF-8 normalizasyonu";
    public string Description => "IL kaynak dosyalarını UTF-8 olarak yeniden yazar.";

    public bool CanProcess(string filePath) =>
        filePath.EndsWith(".il", StringComparison.OrdinalIgnoreCase);

    public string Process(string filePath, string workDirectory, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        byte[] bytes = File.ReadAllBytes(filePath);

        if (LooksLikeUtf8(bytes))
            return filePath;

        Directory.CreateDirectory(workDirectory);
        string normalized = Path.Combine(workDirectory, Path.GetFileName(filePath));
        string text = Encoding.Default.GetString(bytes);
        File.WriteAllText(normalized, text, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
        return normalized;
    }

    private static bool LooksLikeUtf8(byte[] bytes)
    {
        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
            return true;

        try
        {
            var utf8 = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);
            utf8.GetString(bytes);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }
}
