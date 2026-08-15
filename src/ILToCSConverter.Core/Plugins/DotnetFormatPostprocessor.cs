using System.Diagnostics;
using System.Text;
using ILToCSConverter.Core.Conversion;

namespace ILToCSConverter.Core.Plugins;

public sealed class DotnetFormatPostprocessor : IOutputPostprocessor
{
    public string Name => "dotnet format";
    public string Description => "Üretilen C# projesini dotnet format ile düzenler.";

    public void Process(FileConversionResult result, ConversionOptions options, CancellationToken cancellationToken)
    {
        if (!options.FormatOutput || result.Status != ConversionStatus.Succeeded || string.IsNullOrWhiteSpace(result.ProjectFile))
            return;

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

        startInfo.ArgumentList.Add("format");
        startInfo.ArgumentList.Add(result.ProjectFile);
        startInfo.ArgumentList.Add("--severity");
        startInfo.ArgumentList.Add("warn");

        using var process = System.Diagnostics.Process.Start(startInfo);
        if (process is null)
            return;

        using var registration = cancellationToken.Register(() =>
        {
            try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
        });

        process.WaitForExit();
    }
}
