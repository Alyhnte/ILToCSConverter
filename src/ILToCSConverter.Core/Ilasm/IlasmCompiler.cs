using System.Diagnostics;
using System.Text;

namespace ILToCSConverter.Core.Ilasm;

public sealed record IlasmCompileResult(bool Success, string Output, string Error, int ExitCode);

public static class IlasmCompiler
{
    public static IlasmCompileResult Compile(
        string ilasmPath,
        string ilSourcePath,
        string targetDllPath,
        CancellationToken cancellationToken,
        string? snkPath = null)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(targetDllPath)!);

        var startInfo = new ProcessStartInfo
        {
            FileName = ilasmPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        startInfo.ArgumentList.Add("/dll");
        startInfo.ArgumentList.Add($"/output={targetDllPath}");
        if (!string.IsNullOrWhiteSpace(snkPath))
            startInfo.ArgumentList.Add($"/key={snkPath}");
        startInfo.ArgumentList.Add(ilSourcePath);

        using var process = new Process { StartInfo = startInfo };
        var output = new StringBuilder();
        var error = new StringBuilder();

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                lock (output) output.AppendLine(e.Data);
        };
        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null)
                lock (error) error.AppendLine(e.Data);
        };

        if (!process.Start())
            return new IlasmCompileResult(false, "", "ilasm.exe başlatılamadı.", -1);

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var registration = cancellationToken.Register(() =>
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
        });

        if (!process.WaitForExit(TimeSpan.FromMinutes(5)))
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            return new IlasmCompileResult(false, output.ToString(), "ilasm.exe zaman aşımına uğradı (5 dk).", -2);
        }

        process.WaitForExit();

        bool ok = process.ExitCode == 0 && File.Exists(targetDllPath);
        return new IlasmCompileResult(ok, output.ToString().Trim(), error.ToString().Trim(), process.ExitCode);
    }
}
