using System.Security.Cryptography;

namespace ILToCSConverter.Core.Checksums;

public sealed record FileChecksumResult(bool Success, string Sha256, long Length, string? Error);

public static class FileChecksum
{
    public static FileChecksumResult ComputeSha256(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return new FileChecksumResult(false, "", 0, "Dosya bulunamadı");

        try
        {
            using var stream = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite,
                1024 * 64,
                FileOptions.SequentialScan);

            long length = stream.Length;
            byte[] hash = SHA256.HashData(stream);
            return new FileChecksumResult(true, Convert.ToHexString(hash), length, null);
        }
        catch (Exception ex)
        {
            return new FileChecksumResult(false, "", 0, ex.Message);
        }
    }

    public static void WriteSidecar(string outputFolder, string sourcePath, FileChecksumResult checksum)
    {
        Directory.CreateDirectory(outputFolder);
        string name = Path.GetFileName(sourcePath);
        string sidecar = Path.Combine(outputFolder, Path.GetFileNameWithoutExtension(sourcePath) + ".sha256");
        File.WriteAllText(sidecar, $"{checksum.Sha256} *{name}{Environment.NewLine}");
    }

    public static void WriteManifest(string outputFolder, IReadOnlyList<(string SourcePath, FileChecksumResult Checksum)> items)
    {
        Directory.CreateDirectory(outputFolder);
        var lines = new List<string>
        {
            $"# ILToCS Düzün SHA256 — {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            ""
        };

        foreach (var item in items.Where(i => i.Checksum.Success))
        {
            lines.Add($"{item.Checksum.Sha256} *{Path.GetFileName(item.SourcePath)}");
        }

        File.WriteAllLines(Path.Combine(outputFolder, "checksums.sha256"), lines);
    }

    public static void PrependIlHeader(string ilPath, string sourcePath, FileChecksumResult checksum)
    {
        if (!File.Exists(ilPath) || !checksum.Success)
            return;

        string header =
            $"// ILToCS Düzün{Environment.NewLine}" +
            $"// Source: {sourcePath}{Environment.NewLine}" +
            $"// Size: {checksum.Length}{Environment.NewLine}" +
            $"// SHA256: {checksum.Sha256}{Environment.NewLine}" +
            $"//{Environment.NewLine}";

        using (var probe = File.OpenRead(ilPath))
        {
            var buffer = new byte[Math.Min(512, probe.Length)];
            int read = probe.Read(buffer);
            string start = System.Text.Encoding.UTF8.GetString(buffer, 0, read);
            if (start.Contains($"SHA256: {checksum.Sha256}", StringComparison.Ordinal))
                return;
        }

        string temp = ilPath + ".header.tmp";
        using (var output = new FileStream(temp, FileMode.Create, FileAccess.Write, FileShare.None))
        {
            var preamble = System.Text.Encoding.UTF8.GetBytes(header);
            output.Write(preamble);
            using var input = new FileStream(ilPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            input.CopyTo(output);
        }

        File.Delete(ilPath);
        File.Move(temp, ilPath);
    }
}
