using System.Security.Cryptography;
using System.Text;

#pragma warning disable SYSLIB0028

namespace ILToCSConverter.Core.Signing;

public sealed class StrongNameKey
{
    private const uint CALG_RSA_SIGN = 0x00002400;
    private const uint CALG_SHA1 = 0x00008004;

    public required string SnkPath { get; init; }
    public required byte[] PublicBlob { get; init; }
    public required string Token { get; init; }

    public string FormattedPublicKeyHex => FormatHex(PublicBlob, bytesPerLine: 16);
    public string FormattedTokenHex => FormatHex(Convert.FromHexString(Token), bytesPerLine: 8);

    public static StrongNameKey Load(string snkPath)
    {
        if (!File.Exists(snkPath))
            throw new FileNotFoundException("Anahtar dosyası bulunamadı.", snkPath);

        byte[] snkBytes = File.ReadAllBytes(snkPath);
        byte[] fullPublicBlob = ExtractFullEcma335Blob(snkBytes);

        return new StrongNameKey
        {
            SnkPath = Path.GetFullPath(snkPath),
            PublicBlob = fullPublicBlob,
            Token = ComputeToken(fullPublicBlob)
        };
    }

    public static StrongNameKey Create(string snkPath, int keySize = 2048)
    {
        string? dir = Path.GetDirectoryName(Path.GetFullPath(snkPath));
        if (!string.IsNullOrEmpty(dir))
            Directory.CreateDirectory(dir);

        using var rsa = new RSACryptoServiceProvider(keySize);
        byte[] privateBlob = rsa.ExportCspBlob(includePrivateParameters: true);
        File.WriteAllBytes(snkPath, privateBlob);

        return Load(snkPath);
    }

    private static byte[] ExtractFullEcma335Blob(byte[] snkBytes)
    {
        if (snkBytes.Length >= 16 && BitConverter.ToUInt32(snkBytes, 0) == CALG_RSA_SIGN)
            return snkBytes;

        using var rsa = new RSACryptoServiceProvider();
        rsa.ImportCspBlob(snkBytes);
        byte[] rawCspPublicBlob = rsa.ExportCspBlob(includePrivateParameters: false);

        byte[] ecmaBlob = new byte[12 + rawCspPublicBlob.Length];
        BitConverter.GetBytes(CALG_RSA_SIGN).CopyTo(ecmaBlob, 0);
        BitConverter.GetBytes(CALG_SHA1).CopyTo(ecmaBlob, 4);
        BitConverter.GetBytes(rawCspPublicBlob.Length).CopyTo(ecmaBlob, 8);
        Buffer.BlockCopy(rawCspPublicBlob, 0, ecmaBlob, 12, rawCspPublicBlob.Length);

        return ecmaBlob;
    }

    public static string ComputeToken(byte[] fullPublicKeyBlob)
    {
        byte[] hash = SHA1.HashData(fullPublicKeyBlob);
        var token = new byte[8];
        for (int i = 0; i < 8; i++)
            token[i] = hash[hash.Length - 1 - i];

        return Convert.ToHexString(token).ToLowerInvariant();
    }

    public static string ComputeTokenFromIlHex(string publicKeyHex)
    {
        string clean = NormalizeHex(publicKeyHex);
        if (clean.Length == 0 || clean.Length % 2 != 0)
            return string.Empty;

        return ComputeToken(Convert.FromHexString(clean));
    }

    public static string NormalizeHex(string hex)
    {
        var builder = new StringBuilder(hex.Length);
        foreach (char c in hex)
        {
            if (Uri.IsHexDigit(c))
                builder.Append(c);
        }
        return builder.ToString();
    }

    public static string FormatHex(byte[] bytes, int bytesPerLine)
    {
        var builder = new StringBuilder();
        for (int i = 0; i < bytes.Length; i++)
        {
            if (i > 0)
            {
                if (i % bytesPerLine == 0)
                    builder.Append("\r\n\t\t\t\t");
                else
                    builder.Append(' ');
            }

            builder.Append(bytes[i].ToString("X2"));
        }

        return builder.ToString();
    }
}