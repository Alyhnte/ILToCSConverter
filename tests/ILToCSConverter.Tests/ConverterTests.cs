using ILToCSConverter.Core.Conversion;
using ILToCSConverter.Core.Ilasm;

namespace ILToCSConverter.Tests;

public sealed class OutputPathAllocatorTests
{
    [Fact]
    public void Same_file_name_from_different_folders_uses_relative_subfolder()
    {
        string root = Path.Combine(Path.GetTempPath(), "iltocs-alloc-" + Guid.NewGuid().ToString("N"));
        string input = Path.Combine(root, "in");
        string output = Path.Combine(root, "out");
        Directory.CreateDirectory(Path.Combine(input, "a"));
        Directory.CreateDirectory(Path.Combine(input, "b"));
        string first = Path.Combine(input, "a", "Player.il");
        string second = Path.Combine(input, "b", "Player.il");
        File.WriteAllText(first, "");
        File.WriteAllText(second, "");

        var allocator = new OutputPathAllocator();
        string one = allocator.Allocate(output, first, input);
        string two = allocator.Allocate(output, second, input);

        Assert.NotEqual(one, two);
        Assert.Contains($"{Path.DirectorySeparatorChar}a{Path.DirectorySeparatorChar}Player", one);
        Assert.Contains($"{Path.DirectorySeparatorChar}b{Path.DirectorySeparatorChar}Player", two);
    }

    [Fact]
    public void Collision_in_same_folder_appends_hash()
    {
        string root = Path.Combine(Path.GetTempPath(), "iltocs-hash-" + Guid.NewGuid().ToString("N"));
        string output = Path.Combine(root, "out");
        string file = Path.Combine(root, "Player.il");
        Directory.CreateDirectory(root);
        File.WriteAllText(file, "");

        var allocator = new OutputPathAllocator();
        string first = allocator.Allocate(output, file, root);
        Directory.CreateDirectory(first);
        File.WriteAllText(Path.Combine(first, "marker.txt"), "x");
        string second = allocator.Allocate(output, file, root);

        Assert.NotEqual(first, second);
        Assert.Contains("_", Path.GetFileName(second));
    }
}

public sealed class FileDiscoveryTests
{
    [Fact]
    public void Finds_il_dll_exe_and_skips_output_folder()
    {
        string root = Path.Combine(Path.GetTempPath(), "iltocs-disc-" + Guid.NewGuid().ToString("N"));
        string input = Path.Combine(root, "in");
        string output = Path.Combine(root, "in", "out");
        Directory.CreateDirectory(Path.Combine(input, "sub"));
        Directory.CreateDirectory(output);
        File.WriteAllText(Path.Combine(input, "a.il"), "");
        File.WriteAllText(Path.Combine(input, "sub", "b.dll"), "");
        File.WriteAllText(Path.Combine(output, "skip.il"), "");
        File.WriteAllText(Path.Combine(input, "notes.txt"), "");

        var files = FileDiscovery.Discover(input, output, recursive: true, new SilentLogger());

        Assert.Contains(files, f => f.EndsWith("a.il"));
        Assert.Contains(files, f => f.EndsWith("b.dll"));
        Assert.DoesNotContain(files, f => f.EndsWith("skip.il"));
        Assert.DoesNotContain(files, f => f.EndsWith("notes.txt"));
    }
}

public sealed class LanguageVersionMapperTests
{
    [Theory]
    [InlineData("CSharp12", ICSharpCode.Decompiler.CSharp.LanguageVersion.CSharp12_0)]
    [InlineData("10", ICSharpCode.Decompiler.CSharp.LanguageVersion.CSharp10_0)]
    [InlineData("latest", ICSharpCode.Decompiler.CSharp.LanguageVersion.Latest)]
    public void Parses_known_values(string input, ICSharpCode.Decompiler.CSharp.LanguageVersion expected)
    {
        Assert.Equal(expected, LanguageVersionMapper.Parse(input));
    }
}

public sealed class ConverterEngineTests
{
    [Fact]
    public void Converts_hello_il_to_csharp_project()
    {
        if (IlasmLocator.Find() is null)
            return;

        string fixture = Path.Combine(AppContext.BaseDirectory, "Fixtures", "Hello.il");
        Assert.True(File.Exists(fixture), fixture);

        string output = Path.Combine(Path.GetTempPath(), "iltocs-hello-" + Guid.NewGuid().ToString("N"));
        var options = new ConversionOptions
        {
            InputPath = fixture,
            OutputPath = output,
            Recursive = false,
            IncludeGlobalNamespace = true
        };

        var result = new ConverterEngine().Convert(options, progress: null, new SilentLogger(), CancellationToken.None);

        Assert.Equal(1, result.Total);
        Assert.Equal(1, result.Succeeded);
        Assert.True(File.Exists(result.ReportPath));
        string allText = string.Join(Environment.NewLine,
            Directory.GetFiles(output, "*.cs", SearchOption.AllDirectories).Select(File.ReadAllText));
        Assert.Contains("Greeter", allText);
        Assert.Contains("Greet", allText);
        Assert.Contains("GlobalType", allText);
        Assert.NotEmpty(Directory.GetFiles(output, "*.csproj", SearchOption.AllDirectories));
        Assert.True(File.Exists(result.JUnitPath));
        Assert.True(File.Exists(result.SolutionPath));
    }
}

public sealed class TargetFrameworkMapperTests
{
    [Theory]
    [InlineData(".NETCoreApp,Version=v8.0", "net8.0")]
    [InlineData(".NETStandard,Version=v2.1", "netstandard2.1")]
    [InlineData(".NETFramework,Version=v4.8", "net48")]
    [InlineData(".NETFramework,Version=v4.7.2", "net472")]
    public void Maps_framework_ids(string id, string expected)
    {
        Assert.Equal(expected, ILToCSConverter.Core.Decompile.TargetFrameworkMapper.ToSdkMoniker(id));
    }
}

public sealed class ConversionQueueTests
{
    [Fact]
    public void Pause_and_resume_block_and_release()
    {
        var queue = new ConversionQueue();
        Assert.False(queue.IsPaused);
        queue.Pause();
        Assert.True(queue.IsPaused);
        queue.Resume();
        Assert.False(queue.IsPaused);
        queue.WaitIfPaused(CancellationToken.None);
    }
}

public sealed class DisassemblePathTests
{
    [Fact]
    public void Default_output_is_next_to_dll()
    {
        string dll = Path.Combine("C:", "mods", "TaleWorlds.dll");
        string output = ILToCSConverter.Core.Ildasm.DisassembleOptions.DefaultOutputFor(dll);
        Assert.EndsWith("TaleWorlds_IL_Outputs", output);
    }
}

public sealed class FileChecksumTests
{
    [Fact]
    public void Computes_uppercase_sha256_and_allows_shared_read()
    {
        string path = Path.Combine(Path.GetTempPath(), "iltocs-sha-" + Guid.NewGuid().ToString("N") + ".dll");
        byte[] payload = "ILToCS-checksum"u8.ToArray();
        File.WriteAllBytes(path, payload);

        try
        {
            using var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var result = ILToCSConverter.Core.Checksums.FileChecksum.ComputeSha256(path);
            string expected = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(payload));

            Assert.True(result.Success);
            Assert.Equal(payload.Length, result.Length);
            Assert.Equal(expected, result.Sha256);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Missing_file_is_not_returned_as_hash()
    {
        var result = ILToCSConverter.Core.Checksums.FileChecksum.ComputeSha256(Path.Combine(Path.GetTempPath(), "yok.dll"));
        Assert.False(result.Success);
        Assert.Equal("", result.Sha256);
        Assert.Equal("Dosya bulunamadı", result.Error);
    }
}

internal sealed class SilentLogger : IConversionLogger
{
    public void Info(string message) { }
    public void Warn(string message) { }
    public void Success(string message) { }
    public void Error(string message) { }
}
