using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using ILToCSConverter.Core.Conversion;
using ILToCSConverter.Core.Ilasm;
using ILToCSConverter.Core.Reporting;
using ILToCSConverter.Core.Signing;

namespace ILToCSConverter.Core.Packing;

public sealed class PackOptions
{
    public required string InputPath { get; init; }
    public required string OutputPath { get; init; }
    public bool Recursive { get; init; } = true;
    public int MaxParallelism { get; init; }
    public string? IlasmPath { get; init; }
    public string? SnkPath { get; init; }
    public bool GenerateSnkIfMissing { get; init; }
    public bool AlignPublicKey { get; init; } = true;

    public int EffectiveMaxParallelism =>
        MaxParallelism > 0 ? MaxParallelism : Math.Max(1, Environment.ProcessorCount);
}

public sealed class PackEngine
{
    public static readonly string[] IlExtensions = [".il"];

    public ConversionBatchResult Pack(
        PackOptions options,
        IProgress<ConversionProgress>? progress,
        IConversionLogger logger,
        CancellationToken cancellationToken,
        ConversionQueue? queue = null)
    {
        var stopwatch = Stopwatch.StartNew();
        Directory.CreateDirectory(options.OutputPath);

        string? ilasmPath = IlasmLocator.Find(options.IlasmPath);
        if (ilasmPath is null)
        {
            logger.Error("ilasm.exe bulunamadı.");
            var failed = new ConversionBatchResult { Files = [], OutputPath = options.OutputPath, Duration = stopwatch.Elapsed };
            return failed with { ReportPath = ConversionReportWriter.Write(options.OutputPath, failed) };
        }

        logger.Info($"ilasm: {ilasmPath}");

        StrongNameKey? key = null;
        string? snkPath = options.SnkPath;
        if (options.GenerateSnkIfMissing && (string.IsNullOrWhiteSpace(snkPath) || !File.Exists(snkPath)))
        {
            snkPath = string.IsNullOrWhiteSpace(snkPath)
                ? Path.Combine(options.OutputPath, "signing.snk")
                : snkPath;
            key = StrongNameKey.Create(snkPath);
            logger.Info($"Yeni kendi anahtarınız yazıldı: {snkPath}  (token {key.Token})");
        }
        else if (!string.IsNullOrWhiteSpace(snkPath))
        {
            key = StrongNameKey.Load(snkPath);
            logger.Info($"Anahtar: {key.SnkPath}  (token {key.Token})");
        }
        else
        {
            logger.Warn("SNK verilmedi; DLL imzasız üretilecek.");
        }

        var files = FileDiscovery.Discover(options.InputPath, options.OutputPath, options.Recursive, logger, IlExtensions);
        if (files.Count == 0)
        {
            logger.Warn("Paketlenecek .il dosyası bulunamadı.");
            var empty = new ConversionBatchResult { Files = [], OutputPath = options.OutputPath, Duration = stopwatch.Elapsed };
            return empty with { ReportPath = ConversionReportWriter.Write(options.OutputPath, empty) };
        }

        string inputRoot = File.Exists(options.InputPath)
            ? Path.GetDirectoryName(Path.GetFullPath(options.InputPath)) ?? options.OutputPath
            : Path.GetFullPath(options.InputPath);

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
                var result = PackOne(file, inputRoot, options.OutputPath, ilasmPath, key, options.AlignPublicKey, logger, allocator, cancellationToken);
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
                    await channel.Writer.WriteAsync(file, cancellationToken);
                channel.Writer.Complete();
            }, cancellationToken);
            Task.WaitAll(workers.Concat([producer]).ToArray(), cancellationToken);
        }
        catch (OperationCanceledException)
        {
            channel.Writer.TryComplete();
            logger.Warn("Paketleme iptal edildi.");
            throw;
        }

        var batch = new ConversionBatchResult
        {
            Files = results.OrderBy(r => r.SourcePath, StringComparer.OrdinalIgnoreCase).ToArray(),
            OutputPath = options.OutputPath,
            Duration = stopwatch.Elapsed
        };
        string report = ConversionReportWriter.Write(options.OutputPath, batch);
        logger.Info($"Tamamlandı: {batch.Succeeded}/{batch.Total} DLL.");
        return batch with { ReportPath = report, JUnitPath = JUnitReportWriter.Write(options.OutputPath, batch) };
    }

    private static FileConversionResult PackOne(
        string sourcePath,
        string inputRoot,
        string outputRoot,
        string ilasmPath,
        StrongNameKey? key,
        bool align,
        IConversionLogger logger,
        OutputPathAllocator allocator,
        CancellationToken cancellationToken)
    {
        string fileName = Path.GetFileName(sourcePath);
        string folder = allocator.Allocate(outputRoot, sourcePath, inputRoot);
        Directory.CreateDirectory(folder);
        string dllPath = Path.Combine(folder, Path.GetFileNameWithoutExtension(sourcePath) + ".dll");
        string workIl = Path.Combine(folder, Path.GetFileName(sourcePath));

        try
        {
            File.Copy(sourcePath, workIl, overwrite: true);
            string? tokenInfo = null;
            if (key is not null && align)
            {
                var aligned = StrongNameAligner.AlignFile(workIl, key);
                tokenInfo = aligned.OldToken is null
                    ? $"token {aligned.NewToken}"
                    : $"token {aligned.OldToken} → {aligned.NewToken}";
                logger.Info($"{fileName}: kendi anahtarınız uygulandı ({tokenInfo}).");
            }

            var compiled = IlasmCompiler.Compile(ilasmPath, workIl, dllPath, cancellationToken, key?.SnkPath);
            if (!compiled.Success)
            {
                logger.Error($"{fileName}: ilasm başarısız (çıkış {compiled.ExitCode}).");
                return new FileConversionResult
                {
                    SourcePath = sourcePath,
                    OutputFolder = folder,
                    Status = ConversionStatus.Failed,
                    Message = "ilasm.exe DLL üretemedi.",
                    Detail = string.Join(Environment.NewLine, new[] { compiled.Output, compiled.Error }.Where(s => !string.IsNullOrWhiteSpace(s)))
                };
            }

            logger.Success($"{fileName} → {dllPath}");
            return new FileConversionResult
            {
                SourcePath = sourcePath,
                OutputFolder = folder,
                Status = ConversionStatus.Succeeded,
                Message = key is null ? "DLL yazıldı (imzasız)." : $"DLL imzalandı ({tokenInfo ?? key.Token}).",
                GeneratedFileCount = 1
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
