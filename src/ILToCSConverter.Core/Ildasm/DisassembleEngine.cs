using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using ILToCSConverter.Core.Checksums;
using ILToCSConverter.Core.Conversion;
using ILToCSConverter.Core.Reporting;

namespace ILToCSConverter.Core.Ildasm;

public sealed class DisassembleOptions
{
    public required string InputPath { get; init; }
    public string? OutputPath { get; init; }
    public bool Recursive { get; init; } = true;
    public int MaxParallelism { get; init; }
    public string? IldasmPath { get; init; }

    public int EffectiveMaxParallelism =>
        MaxParallelism > 0 ? MaxParallelism : Math.Max(1, Environment.ProcessorCount);

    public static string DefaultOutputFor(string inputPath)
    {
        string full = Path.GetFullPath(inputPath);
        string ext = Path.GetExtension(full);
        bool looksLikeAssembly = DisassembleEngine.AssemblyExtensions.Any(e =>
            e.Equals(ext, StringComparison.OrdinalIgnoreCase));

        if (looksLikeAssembly || File.Exists(full))
        {
            string? dir = Path.GetDirectoryName(full);
            return Path.Combine(dir ?? Directory.GetCurrentDirectory(), Path.GetFileNameWithoutExtension(full) + "_IL_Outputs");
        }

        return full.TrimEnd(Path.DirectorySeparatorChar) + "_IL_Outputs";
    }
}

public sealed class DisassembleEngine
{
    public static readonly string[] AssemblyExtensions = [".dll", ".exe", ".netmodule"];

    public ConversionBatchResult Disassemble(
        DisassembleOptions options,
        IProgress<ConversionProgress>? progress,
        IConversionLogger logger,
        CancellationToken cancellationToken,
        ConversionQueue? queue = null)
    {
        var stopwatch = Stopwatch.StartNew();
        string outputPath = string.IsNullOrWhiteSpace(options.OutputPath)
            ? DisassembleOptions.DefaultOutputFor(options.InputPath)
            : Path.GetFullPath(options.OutputPath);

        Directory.CreateDirectory(outputPath);

        string? ildasmPath = IldasmLocator.Find(options.IldasmPath);
        if (ildasmPath is null)
        {
            logger.Error("ildasm.exe bulunamadı. Windows SDK / NETFX Tools kurulu olmalı.");
            var failed = new ConversionBatchResult { Files = [], OutputPath = outputPath, Duration = stopwatch.Elapsed };
            return failed with { ReportPath = ConversionReportWriter.Write(outputPath, failed) };
        }

        logger.Info($"ildasm: {ildasmPath}");

        var files = FileDiscovery.Discover(options.InputPath, outputPath, options.Recursive, logger, AssemblyExtensions);
        if (files.Count == 0)
        {
            logger.Warn("Düzülecek DLL/EXE bulunamadı.");
            var empty = new ConversionBatchResult { Files = [], OutputPath = outputPath, Duration = stopwatch.Elapsed };
            return empty with { ReportPath = ConversionReportWriter.Write(outputPath, empty) };
        }

        string inputRoot = File.Exists(options.InputPath)
            ? Path.GetDirectoryName(Path.GetFullPath(options.InputPath)) ?? outputPath
            : Path.GetFullPath(options.InputPath);

        logger.Info($"{files.Count} assembly düzülüyor. Paralellik: {options.EffectiveMaxParallelism}");

        int workersCount = Math.Max(1, Math.Min(options.EffectiveMaxParallelism, files.Count));
        var allocator = new OutputPathAllocator();
        var results = new ConcurrentBag<FileConversionResult>();
        int completed = 0;

        var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(Math.Max(4, workersCount * 2))
        {
            SingleWriter = true,
            SingleReader = false,
            FullMode = BoundedChannelFullMode.Wait
        });

        var workers = Enumerable.Range(0, workersCount).Select(_ => Task.Run(async () =>
        {
            await foreach (string file in channel.Reader.ReadAllAsync(cancellationToken))
            {
                queue?.WaitIfPaused(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();

                var result = DisassembleOne(file, inputRoot, outputPath, ildasmPath, allocator, logger, cancellationToken);
                results.Add(result);
                int done = Interlocked.Increment(ref completed);
                progress?.Report(new ConversionProgress(done, files.Count, Path.GetFileName(file), result.Message ?? "", result.Status));
            }
        }, cancellationToken)).ToArray();

        try
        {
            var producer = Task.Run(async () =>
            {
                foreach (string file in files)
                    await channel.Writer.WriteAsync(file, cancellationToken).ConfigureAwait(false);
                channel.Writer.Complete();
            }, cancellationToken);

            Task.WaitAll(workers.Concat([producer]).ToArray(), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            channel.Writer.TryComplete();
            logger.Warn("Düzün işlemi iptal edildi.");
            throw;
        }

        var batch = new ConversionBatchResult
        {
            Files = results.OrderBy(r => r.SourcePath, StringComparer.OrdinalIgnoreCase).ToArray(),
            OutputPath = outputPath,
            Duration = stopwatch.Elapsed
        };

        string report = ConversionReportWriter.Write(outputPath, batch);
        string junit = JUnitReportWriter.Write(outputPath, batch);
        FileChecksum.WriteManifest(
            outputPath,
            batch.Files
                .Where(f => !string.IsNullOrWhiteSpace(f.Sha256))
                .Select(f => (f.SourcePath, new FileChecksumResult(true, f.Sha256!, f.SourceLength, null)))
                .ToArray());
        logger.Info($"Tamamlandı: {batch.Succeeded}/{batch.Total} başarılı.");
        logger.Info($"Rapor: {report}");
        logger.Info($"SHA256 listesi: {Path.Combine(outputPath, "checksums.sha256")}");
        return batch with { ReportPath = report, JUnitPath = junit };
    }

    private static FileConversionResult DisassembleOne(
        string sourcePath,
        string inputRoot,
        string outputRoot,
        string ildasmPath,
        OutputPathAllocator allocator,
        IConversionLogger logger,
        CancellationToken cancellationToken)
    {
        string fileName = Path.GetFileName(sourcePath);
        string folder = allocator.Allocate(outputRoot, sourcePath, inputRoot);
        Directory.CreateDirectory(folder);
        string ilPath = Path.Combine(folder, Path.GetFileNameWithoutExtension(sourcePath) + ".il");

        try
        {
            var checksum = FileChecksum.ComputeSha256(sourcePath);
            if (checksum.Success)
                logger.Info($"{fileName} SHA256: {checksum.Sha256}");
            else
                logger.Warn($"{fileName} SHA256 alınamadı: {checksum.Error}");

            var result = IldasmDisassembler.Disassemble(ildasmPath, sourcePath, ilPath, cancellationToken);
            if (!result.Success)
            {
                logger.Error($"{fileName}: ildasm başarısız (çıkış {result.ExitCode}).");
                return new FileConversionResult
                {
                    SourcePath = sourcePath,
                    OutputFolder = folder,
                    Status = ConversionStatus.Failed,
                    Message = "ildasm.exe bu dosyayı düzemedi.",
                    Detail = string.Join(Environment.NewLine, new[] { result.Output, result.Error }.Where(s => !string.IsNullOrWhiteSpace(s))),
                    Sha256 = checksum.Success ? checksum.Sha256 : null,
                    SourceLength = checksum.Length
                };
            }

            if (checksum.Success)
            {
                FileChecksum.WriteSidecar(folder, sourcePath, checksum);
                FileChecksum.PrependIlHeader(ilPath, sourcePath, checksum);
            }

            logger.Success($"{fileName} → {ilPath}");
            return new FileConversionResult
            {
                SourcePath = sourcePath,
                OutputFolder = folder,
                Status = ConversionStatus.Succeeded,
                Message = checksum.Success ? $"IL yazıldı. SHA256 {checksum.Sha256}" : "IL yazıldı.",
                GeneratedFileCount = 1,
                Sha256 = checksum.Success ? checksum.Sha256 : null,
                SourceLength = checksum.Length
            };
        }
        catch (OperationCanceledException)
        {
            return new FileConversionResult
            {
                SourcePath = sourcePath,
                OutputFolder = folder,
                Status = ConversionStatus.Cancelled,
                Message = "İptal edildi."
            };
        }
        catch (Exception ex)
        {
            logger.Error($"{fileName}: {ex.Message}");
            return new FileConversionResult
            {
                SourcePath = sourcePath,
                OutputFolder = folder,
                Status = ConversionStatus.Failed,
                Message = ex.Message,
                Detail = ex.ToString()
            };
        }
    }
}
