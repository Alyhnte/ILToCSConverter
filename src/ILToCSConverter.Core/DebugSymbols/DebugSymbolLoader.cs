using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;
using ICSharpCode.Decompiler.DebugInfo;
using ICSharpCode.Decompiler.Metadata;

namespace ILToCSConverter.Core.DebugSymbols;

public sealed class LoadedDebugSymbols : IDisposable
{
    public IDebugInfoProvider? Provider { get; init; }
    public MetadataReaderProvider? ReaderProvider { get; init; }
    public string Description { get; init; } = "PDB yok";

    public void Dispose() => ReaderProvider?.Dispose();
}

public static class DebugSymbolLoader
{
    public static LoadedDebugSymbols TryLoad(PEFile module)
    {
        try
        {
            if (module.Reader.TryOpenAssociatedPortablePdb(
                    module.FileName,
                    OpenIfExists,
                    out MetadataReaderProvider? provider,
                    out string? pdbPath)
                && provider is not null)
            {
                return new LoadedDebugSymbols
                {
                    ReaderProvider = provider,
                    Provider = new PortablePdbDebugInfoProvider(module.FileName, provider, pdbPath),
                    Description = string.IsNullOrEmpty(pdbPath) ? "Gömülü PDB" : pdbPath
                };
            }
        }
        catch (BadImageFormatException)
        {
            // Eski Windows PDB veya bozuk akış.
        }

        string sidecar = Path.ChangeExtension(module.FileName, ".pdb");
        if (File.Exists(sidecar) && !IsLegacyWindowsPdb(sidecar))
        {
            try
            {
                var stream = File.OpenRead(sidecar);
                var provider = MetadataReaderProvider.FromPortablePdbStream(stream, MetadataStreamOptions.PrefetchMetadata);
                return new LoadedDebugSymbols
                {
                    ReaderProvider = provider,
                    Provider = new PortablePdbDebugInfoProvider(module.FileName, provider, sidecar),
                    Description = sidecar
                };
            }
            catch (Exception)
            {
                // Portable PDB değilse sessizce geç.
            }
        }

        return new LoadedDebugSymbols();
    }

    private static Stream? OpenIfExists(string path)
    {
        if (!File.Exists(path))
            return null;
        var memory = new MemoryStream();
        using var file = File.OpenRead(path);
        file.CopyTo(memory);
        memory.Position = 0;
        return memory;
    }

    private static bool IsLegacyWindowsPdb(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            Span<byte> header = stackalloc byte[24];
            int read = stream.Read(header);
            if (read < 24)
                return false;
            string text = System.Text.Encoding.ASCII.GetString(header);
            return text.StartsWith("Microsoft C/C++ MSF", StringComparison.Ordinal);
        }
        catch
        {
            return false;
        }
    }
}
