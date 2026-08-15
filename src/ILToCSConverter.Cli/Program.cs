using System.CommandLine;
using ILToCSConverter.Core;
using ILToCSConverter.Core.Conversion;
using ILToCSConverter.Core.Ilasm;
using ILToCSConverter.Core.Ildasm;
using ILToCSConverter.Core.Packing;
using ILToCSConverter.Core.Settings;

namespace ILToCSConverter.Cli;

internal static class Program
{
    public static int Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        if (args.Length == 0)
            return RunInteractive();

        var inputOption = CreateInputOption("Kaynak dosya veya klasör (.il, .dll, .exe, .netmodule)");
        var outputOption = new Option<string>("--out", "-o")
        {
            Description = "C# projesinin yazılacağı klasör",
            Required = true
        };
        var recursiveOption = CreateRecursiveOption();
        var parallelOption = CreateParallelOption();
        var langOption = new Option<string>("--lang")
        {
            Description = "C# dil sürümü: CSharp73, CSharp10, CSharp11, CSharp12, Latest",
            DefaultValueFactory = _ => "CSharp12"
        };
        var ilasmOption = new Option<string?>("--ilasm") { Description = "ilasm.exe yolu" };
        var verifyOption = new Option<bool>("--verify-build") { Description = "dotnet build ile doğrula" };
        var formatOption = new Option<bool>("--format") { Description = "dotnet format uygula" };
        var includeGlobalOption = new Option<bool>("--include-global")
        {
            Description = "Global isim alanındaki tipleri yaz",
            DefaultValueFactory = _ => true
        };
        var includeGeneratedOption = new Option<bool>("--include-compiler-generated")
        {
            Description = "Derleyici üretimi tipleri de yaz"
        };
        var noSlnOption = new Option<bool>("--no-sln") { Description = ".sln üretme" };
        var noBamlOption = new Option<bool>("--no-baml") { Description = "BAML → XAML kapalı" };
        var noPdbOption = new Option<bool>("--no-pdb") { Description = "PDB satır eşlemesini kullanma" };

        var root = new RootCommand($"{ProductInfo.Name} — IL/DLL/EXE → C#, Düzün (DLL → IL) ve IL → DLL")
        {
            inputOption,
            outputOption,
            recursiveOption,
            parallelOption,
            langOption,
            ilasmOption,
            verifyOption,
            formatOption,
            includeGlobalOption,
            includeGeneratedOption,
            noSlnOption,
            noBamlOption,
            noPdbOption
        };

        root.SetAction(parseResult =>
        {
            var options = new ConversionOptions
            {
                InputPath = parseResult.GetRequiredValue(inputOption),
                OutputPath = parseResult.GetRequiredValue(outputOption),
                Recursive = parseResult.GetValue(recursiveOption),
                MaxParallelism = parseResult.GetValue(parallelOption),
                LanguageVersion = parseResult.GetValue(langOption) ?? "CSharp12",
                IlasmPath = parseResult.GetValue(ilasmOption),
                VerifyBuild = parseResult.GetValue(verifyOption),
                FormatOutput = parseResult.GetValue(formatOption),
                IncludeGlobalNamespace = parseResult.GetValue(includeGlobalOption),
                IncludeCompilerGenerated = parseResult.GetValue(includeGeneratedOption),
                GenerateSolution = !parseResult.GetValue(noSlnOption),
                DecompileBaml = !parseResult.GetValue(noBamlOption),
                UsePdb = !parseResult.GetValue(noPdbOption)
            };
            return RunCSharp(options);
        });

        var duzunIn = CreateInputOption("Kaynak DLL/EXE veya klasör");
        var duzunOut = new Option<string?>("--out", "-o")
        {
            Description = "IL çıktı klasörü (boşsa DLL yanında *_IL_Outputs)"
        };
        var duzunIldasm = new Option<string?>("--ildasm") { Description = "ildasm.exe yolu" };
        var duzunRecursive = CreateRecursiveOption();
        var duzunParallel = CreateParallelOption();

        var duzun = new Command("duzun", "DLL/EXE dosyasını IL metnine düzün (ildasm)")
        {
            duzunIn,
            duzunOut,
            duzunIldasm,
            duzunRecursive,
            duzunParallel
        };
        duzun.SetAction(parseResult =>
        {
            var options = new DisassembleOptions
            {
                InputPath = parseResult.GetRequiredValue(duzunIn),
                OutputPath = parseResult.GetValue(duzunOut),
                IldasmPath = parseResult.GetValue(duzunIldasm),
                Recursive = parseResult.GetValue(duzunRecursive),
                MaxParallelism = parseResult.GetValue(duzunParallel)
            };
            return RunDuzun(options);
        });
        root.Subcommands.Add(duzun);

        var packIn = CreateInputOption("Kaynak .il dosyası veya klasör");
        var packOut = new Option<string>("--out", "-o")
        {
            Description = "DLL çıktı klasörü",
            Required = true
        };
        var packSnk = new Option<string?>("--snk") { Description = "Kendi .snk anahtarınız" };
        var packGen = new Option<bool>("--generate-snk") { Description = "Yoksa yeni .snk üret" };
        var packIlasm = new Option<string?>("--ilasm") { Description = "ilasm.exe yolu" };
        var packRecursive = CreateRecursiveOption();
        var packParallel = CreateParallelOption();
        var pack = new Command("pack", "Kendi IL dosyanızı DLL'e derler ve kendi .snk anahtarınızla imzalar")
        {
            packIn, packOut, packSnk, packGen, packIlasm, packRecursive, packParallel
        };
        pack.SetAction(parseResult =>
        {
            var options = new PackOptions
            {
                InputPath = parseResult.GetRequiredValue(packIn),
                OutputPath = parseResult.GetRequiredValue(packOut),
                SnkPath = parseResult.GetValue(packSnk),
                GenerateSnkIfMissing = parseResult.GetValue(packGen),
                IlasmPath = parseResult.GetValue(packIlasm),
                Recursive = parseResult.GetValue(packRecursive),
                MaxParallelism = parseResult.GetValue(packParallel)
            };
            return RunPack(options);
        });
        root.Subcommands.Add(pack);

        return root.Parse(args).Invoke();
    }

    private static Option<string> CreateInputOption(string description) => new("--in", "-i")
    {
        Description = description,
        Required = true
    };

    private static Option<bool> CreateRecursiveOption() => new("--recursive", "-r")
    {
        Description = "Alt klasörleri de tara",
        DefaultValueFactory = _ => true
    };

    private static Option<int> CreateParallelOption() => new("--parallel", "-p")
    {
        Description = "Aynı anda işlenecek dosya sayısı (0 = CPU sayısı)",
        DefaultValueFactory = _ => 0
    };

    private static int RunInteractive()
    {
        Console.WriteLine($"{ProductInfo.Name} {ProductInfo.Version}");
        Console.WriteLine("1) C#'a çevir    2) Düzün (DLL → IL)    3) IL → DLL (kendi anahtar)");
        Console.Write("Seçim [1]: ");
        string? choice = Console.ReadLine();

        Console.Write("Kaynak klasör veya dosya: ");
        string? input = Console.ReadLine()?.Trim(' ', '"');
        Console.Write("Hedef klasör (Düzün'de boş bırakılabilir): ");
        string? output = Console.ReadLine()?.Trim(' ', '"');

        if (string.IsNullOrWhiteSpace(input))
        {
            Console.WriteLine("Kaynak zorunludur.");
            return 1;
        }

        if (choice?.Trim() == "2")
        {
            return RunDuzun(new DisassembleOptions
            {
                InputPath = input,
                OutputPath = string.IsNullOrWhiteSpace(output) ? null : output
            });
        }

        if (choice?.Trim() == "3")
        {
            if (string.IsNullOrWhiteSpace(output))
            {
                Console.WriteLine("IL → DLL için hedef klasör zorunludur.");
                return 1;
            }

            Console.Write(".snk yolu (boşsa yeni üretilir): ");
            string? snk = Console.ReadLine()?.Trim(' ', '"');
            return RunPack(new PackOptions
            {
                InputPath = input,
                OutputPath = output,
                SnkPath = string.IsNullOrWhiteSpace(snk) ? null : snk,
                GenerateSnkIfMissing = string.IsNullOrWhiteSpace(snk)
            });
        }

        if (string.IsNullOrWhiteSpace(output))
        {
            Console.WriteLine("C# dönüşümü için hedef klasör zorunludur.");
            return 1;
        }

        return RunCSharp(SettingsStore.Load().ToOptions(input, output));
    }

    private static int RunCSharp(ConversionOptions options)
    {
        var logger = new ConsoleConversionLogger();
        logger.Info($"{ProductInfo.Name} {ProductInfo.Version}");
        var progress = new Progress<ConversionProgress>(p =>
            Console.WriteLine($"  ({p.Completed}/{p.Total}) {p.CurrentFile}: {p.Message}"));

        try
        {
            var result = new ConverterEngine().Convert(options, progress, logger, CancellationToken.None);
            return result.Failed > 0 ? 2 : 0;
        }
        catch (Exception ex)
        {
            logger.Error($"Kritik hata: {ex.Message}");
            return 1;
        }
    }

    private static int RunDuzun(DisassembleOptions options)
    {
        var logger = new ConsoleConversionLogger();
        logger.Info($"{ProductInfo.Name} {ProductInfo.Version} — Düzün");
        var progress = new Progress<ConversionProgress>(p =>
            Console.WriteLine($"  ({p.Completed}/{p.Total}) {p.CurrentFile}: {p.Message}"));

        try
        {
            var result = new DisassembleEngine().Disassemble(options, progress, logger, CancellationToken.None);
            return result.Failed > 0 ? 2 : 0;
        }
        catch (Exception ex)
        {
            logger.Error($"Kritik hata: {ex.Message}");
            return 1;
        }
    }

    private static int RunPack(PackOptions options)
    {
        var logger = new ConsoleConversionLogger();
        logger.Info($"{ProductInfo.Name} {ProductInfo.Version} — IL → DLL");
        var progress = new Progress<ConversionProgress>(p =>
            Console.WriteLine($"  ({p.Completed}/{p.Total}) {p.CurrentFile}: {p.Message}"));

        try
        {
            var result = new PackEngine().Pack(options, progress, logger, CancellationToken.None);
            return result.Failed > 0 ? 2 : 0;
        }
        catch (Exception ex)
        {
            logger.Error($"Kritik hata: {ex.Message}");
            return 1;
        }
    }
}
