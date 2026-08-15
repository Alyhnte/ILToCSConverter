using System.Diagnostics;
using System.Text;

namespace ILToCSConverter.Core.Ildasm;

public sealed record IldasmResult(bool Success, string Output, string Error, int ExitCode);

public static class IldasmDisassembler
{
    public static IldasmResult Disassemble(string ildasmPath, string assemblyPath, string targetIlPath, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(targetIlPath)!);

        var startInfo = new ProcessStartInfo
        {
            FileName = ildasmPath,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        startInfo.ArgumentList.Add("/text");
        startInfo.ArgumentList.Add("/utf8");
        startInfo.ArgumentList.Add("/nobar");
        startInfo.ArgumentList.Add($"/output={targetIlPath}");
        startInfo.ArgumentList.Add(assemblyPath);

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
            return new IldasmResult(false, "", "ildasm.exe başlatılamadı.", -1);

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        using var registration = cancellationToken.Register(() =>
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
        });

        if (!process.WaitForExit(TimeSpan.FromMinutes(5)))
        {
            try { process.Kill(entireProcessTree: true); } catch { }
            return new IldasmResult(false, output.ToString(), "ildasm.exe zaman aşımına uğradı (5 dk).", -2);
        }

        process.WaitForExit();
        bool ok = process.ExitCode == 0 && File.Exists(targetIlPath) && new FileInfo(targetIlPath).Length > 0;
        return new IldasmResult(ok, output.ToString().Trim(), error.ToString().Trim(), process.ExitCode);
    }
}
