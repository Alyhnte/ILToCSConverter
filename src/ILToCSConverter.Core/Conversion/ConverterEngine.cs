using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading.Channels;
using ILToCSConverter.Core.Build;
using ILToCSConverter.Core.Decompile;
using ILToCSConverter.Core.Ilasm;
using ILToCSConverter.Core.Plugins;
using ILToCSConverter.Core.Reporting;

namespace ILToCSConverter.Core.Conversion;

public sealed class ConverterEngine
{
    private const long LargeAssemblyBytes = 40L * 1024 * 1024;
    private readonly ProjectDecompilerHost _decompiler = new();

    public ConversionBatchResult Convert(
        ConversionOptions options,
        IProgress<ConversionProgress>? progress,
        IConversionLogger logger,
        CancellationToken cancellationToken,
        ConversionQueue? queue = null)
    {
        var stopwatch = Stopwatch.StartNew();
        Directory.CreateDirectory(options.OutputPath);

        string? ilasmPath = IlasmLocator.Find(options.IlasmPath);
        if (ilasmPath is not null)
            logger.Info($"ilasm: {ilasmPath}");
        else
            logger.Warn("ilasm.exe bulunamadı. Yalnızca DLL/EXE/netmodule dosyaları dönüştürülebilir.");

        var plugins = PluginCatalog.CreateDefault(
            Path.Combine(AppContext.BaseDirectory, "plugins"),
            new LoggerAdapter(logger));

        var files = FileDiscovery.Discover(options.InputPath, options.OutputPath, options.Recursive, logger);
        if (files.Count == 0)
        {
            logger.Warn("Dönüştürülecek dosya bulunamadı. Desteklenen türler: .il, .dll, .exe, .netmodule");
            var empty = new ConversionBatchResult
            {
                Files = [],
                OutputPath = options.OutputPath,
                Duration = stopwatch.Elapsed
            };
            empty = empty with { ReportPath = ConversionReportWriter.Write(options.OutputPath, empty) };
            return empty;
        }

        string inputRoot = File.Exists(options.InputPath)
            ? Path.GetDirectoryName(Path.GetFullPath(options.InputPath)) ?? options.OutputPath
            : Path.GetFullPath(options.InputPath);

        logger.Info($"{files.Count} dosya bulundu. Paralellik: {options.EffectiveMaxParallelism} işçi.");

        int fileWorkers = Math.Max(1, Math.Min(options.EffectiveMaxParallelism, files.Count));
        if (files.Any(path => new FileInfo(path).Length >= LargeAssemblyBytes))
            fileWorkers = Math.Min(fileWorkers, 2);

        int perProjectParallelism = Math.Max(1, options.EffectiveMaxParallelism / fileWorkers);
        var allocator = new OutputPathAllocator();
        var results = new ConcurrentBag<FileConversionResult>();
        int completed = 0;

        var channel = Channel.CreateBounded<string>(new BoundedChannelOptions(Math.Max(4, fileWorkers * 2))
        {
            SingleWriter = true,
            SingleReader = false,
            FullMode = BoundedChannelFullMode.Wait
        });

        var workers = Enumerable.Range(0, fileWorkers).Select(_ => Task.Run(async () =>
        {
            await foreach (string file in channel.Reader.ReadAllAsync(cancellationToken))
            {
                queue?.WaitIfPaused(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
                var result = ConvertSingle(
                    file,
                    inputRoot,
                    options,
                    allocator,
                    ilasmPath,
                    perProjectParallelism,
                    plugins,
                    logger,
                    cancellationToken);

                results.Add(result);
                int done = Interlocked.Increment(ref completed);
                progress?.Report(new ConversionProgress(
                    done,
                    files.Count,
                    Path.GetFileName(file),
                    result.Message ?? result.Status.ToString(),
                    result.Status));
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
            logger.Warn("Dönüştürme iptal edildi.");
            throw;
        }

        var ordered = results
            .OrderBy(r => r.SourcePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var batch = new ConversionBatchResult
        {
            Files = ordered,
            OutputPath = options.OutputPath,
            Duration = stopwatch.Elapsed
        };

        string reportPath = options.ReportPath ?? ConversionReportWriter.Write(options.OutputPath, batch);
        string? junitPath = options.WriteJUnitReport ? JUnitReportWriter.Write(options.OutputPath, batch) : null;
        string? slnPath = options.GenerateSolution ? SolutionGenerator.Write(options.OutputPath, ordered) : null;
        batch = batch with { ReportPath = reportPath, JUnitPath = junitPath, SolutionPath = slnPath };

        logger.Info($"Tamamlandı: {batch.Succeeded}/{batch.Total} başarılı, {batch.Failed} hatalı, {batch.Skipped} atlanan.");
        logger.Info($"Rapor: {reportPath}");
        if (junitPath is not null)
            logger.Info($"JUnit: {junitPath}");
        if (slnPath is not null)
            logger.Info($"Çözüm: {slnPath}");
        return batch;
    }

    private FileConversionResult ConvertSingle(
        string sourcePath,
        string inputRoot,
        ConversionOptions options,
        OutputPathAllocator allocator,
        string? ilasmPath,
        int perProjectParallelism,
        PluginCatalog plugins,
        IConversionLogger logger,
        CancellationToken cancellationToken)
    {
        string fileName = Path.GetFileName(sourcePath);
        string outputFolder = allocator.Allocate(options.OutputPath, sourcePath, inputRoot);
        string workDir = Path.Combine(Path.GetTempPath(), "ILToCS", Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(workDir);
            Directory.CreateDirectory(outputFolder);

            string workingPath = sourcePath;
            foreach (var preprocessor in plugins.Preprocessors)
            {
                if (preprocessor.CanProcess(workingPath))
                    workingPath = preprocessor.Process(workingPath, workDir, cancellationToken);
            }

            string assemblyPath;
            string? ilasmLog = null;

            if (workingPath.EndsWith(".il", StringComparison.OrdinalIgnoreCase))
            {
                if (ilasmPath is null)
                {
                    logger.Error($"{fileName}: ilasm.exe yok, IL derlenemedi.");
                    return Fail(sourcePath, outputFolder, "ilasm.exe bulunamadı.", null);
                }

                string tempDll = Path.Combine(workDir, Path.GetFileNameWithoutExtension(workingPath) + ".dll");
                var compiled = IlasmCompiler.Compile(ilasmPath, workingPath, tempDll, cancellationToken);
                ilasmLog = JoinLogs(compiled.Output, compiled.Error);
                if (!compiled.Success)
                {
                    logger.Error($"{fileName}: ilasm derlemesi başarısız (çıkış {compiled.ExitCode}).");
                    return Fail(sourcePath, outputFolder, "ilasm.exe bu dosyayı derleyemedi.", ilasmLog);
                }

                assemblyPath = tempDll;
            }
            else
            {
                assemblyPath = workingPath;
            }

            int generated = _decompiler.DecompileAssembly(
                assemblyPath,
                outputFolder,
                options,
                perProjectParallelism,
                logger,
                cancellationToken);

            string? projectFile = Directory.EnumerateFiles(outputFolder, "*.csproj", SearchOption.TopDirectoryOnly).FirstOrDefault();
            bool buildOk = false;
            string? buildLog = null;

            if (options.VerifyBuild && projectFile is not null)
            {
                var build = ProjectBuildVerifier.Verify(projectFile, cancellationToken);
                buildOk = build.Succeeded;
                buildLog = build.Output;
                if (!buildOk)
                    logger.Warn($"{fileName}: C# üretildi fakat dotnet build başarısız.");
            }

            var result = new FileConversionResult
            {
                SourcePath = sourcePath,
                OutputFolder = outputFolder,
                ProjectFile = projectFile,
                Status = ConversionStatus.Succeeded,
                Message = $"{generated} dosya üretildi" + (options.VerifyBuild ? (buildOk ? ", derleme tamam." : ", derleme hatalı.") : "."),
                Detail = JoinLogs(ilasmLog, buildLog),
                GeneratedFileCount = generated,
                BuildSucceeded = buildOk || !options.VerifyBuild
            };

            foreach (var post in plugins.Postprocessors)
                post.Process(result, options, cancellationToken);

            logger.Success($"{fileName} → {outputFolder} ({generated} dosya)");
            return result;
        }
        catch (OperationCanceledException)
        {
            return new FileConversionResult
            {
                SourcePath = sourcePath,
                OutputFolder = outputFolder,
                Status = ConversionStatus.Cancelled,
                Message = "İptal edildi."
            };
        }
        catch (BadImageFormatException ex)
        {
            logger.Error($"{fileName}: yönetilen assembly değil. {ex.Message}");
            return Fail(sourcePath, outputFolder, "Yönetilen .NET assembly'si değil.", ex.Message);
        }
        catch (Exception ex)
        {
            logger.Error($"{fileName}: {ex.Message}");
            return Fail(sourcePath, outputFolder, ex.Message, ex.ToString());
        }
        finally
        {
            TryDeleteDirectory(workDir);
        }
    }

    private static FileConversionResult Fail(string source, string outputFolder, string message, string? detail) => new()
    {
        SourcePath = source,
        OutputFolder = outputFolder,
        Status = ConversionStatus.Failed,
        Message = message,
        Detail = detail
    };

    private static string? JoinLogs(params string?[] parts)
    {
        var text = string.Join(Environment.NewLine, parts.Where(p => !string.IsNullOrWhiteSpace(p)));
        return string.IsNullOrWhiteSpace(text) ? null : text;
    }

    private static void TryDeleteDirectory(string path)
    {
        for (int i = 0; i < 3; i++)
        {
            try
            {
                if (Directory.Exists(path))
                    Directory.Delete(path, recursive: true);
                return;
            }
            catch
            {
                Thread.Sleep(80);
            }
        }
    }
}
