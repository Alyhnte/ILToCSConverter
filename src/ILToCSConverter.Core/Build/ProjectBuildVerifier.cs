using System.Diagnostics;
using System.Text;

namespace ILToCSConverter.Core.Build;

public sealed record BuildVerificationResult(bool Succeeded, string Output);

public static class ProjectBuildVerifier
{
    public static BuildVerificationResult Verify(string projectFile, CancellationToken cancellationToken)
    {
        if (!File.Exists(projectFile))
            return new BuildVerificationResult(false, "Proje dosyası bulunamadı.");

        var startInfo = new ProcessStartInfo
        {
            FileName = "dotnet",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        startInfo.ArgumentList.Add("build");
        startInfo.ArgumentList.Add(projectFile);
        startInfo.ArgumentList.Add("--nologo");
        startInfo.ArgumentList.Add("-v");
        startInfo.ArgumentList.Add("q");

        using var process = Process.Start(startInfo);
        if (process is null)
            return new BuildVerificationResult(false, "dotnet başlatılamadı.");

        using var registration = cancellationToken.Register(() =>
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
        });

        string output = process.StandardOutput.ReadToEnd();
        string error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        string combined = string.Join(Environment.NewLine, new[] { output, error }.Where(s => !string.IsNullOrWhiteSpace(s)));
        return new BuildVerificationResult(process.ExitCode == 0, combined.Trim());
    }
}
