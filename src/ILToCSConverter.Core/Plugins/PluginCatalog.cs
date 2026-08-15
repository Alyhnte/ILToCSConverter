using System.Reflection;
using System.Runtime.Loader;

namespace ILToCSConverter.Core.Plugins;

public sealed class PluginCatalog
{
    public IReadOnlyList<IInputPreprocessor> Preprocessors { get; }
    public IReadOnlyList<IOutputPostprocessor> Postprocessors { get; }

    public PluginCatalog(
        IReadOnlyList<IInputPreprocessor> preprocessors,
        IReadOnlyList<IOutputPostprocessor> postprocessors)
    {
        Preprocessors = preprocessors;
        Postprocessors = postprocessors;
    }

    public static PluginCatalog CreateDefault(string? pluginsDirectory, IConversionLoggerAdapter? log = null)
    {
        var preprocessors = new List<IInputPreprocessor> { new Utf8InputPreprocessor() };
        var postprocessors = new List<IOutputPostprocessor> { new DotnetFormatPostprocessor() };

        if (!string.IsNullOrWhiteSpace(pluginsDirectory) && Directory.Exists(pluginsDirectory))
        {
            foreach (string dll in Directory.GetFiles(pluginsDirectory, "*.dll"))
            {
                try
                {
                    var context = new AssemblyLoadContext($"plugin-{Path.GetFileNameWithoutExtension(dll)}", isCollectible: false);
                    Assembly assembly = context.LoadFromAssemblyPath(Path.GetFullPath(dll));
                    foreach (Type type in assembly.GetExportedTypes().Where(t => t is { IsAbstract: false, IsInterface: false }))
                    {
                        if (typeof(IInputPreprocessor).IsAssignableFrom(type))
                            preprocessors.Add((IInputPreprocessor)Activator.CreateInstance(type)!);
                        else if (typeof(IOutputPostprocessor).IsAssignableFrom(type))
                            postprocessors.Add((IOutputPostprocessor)Activator.CreateInstance(type)!);
                    }
                }
                catch (Exception ex)
                {
                    log?.Warn($"Eklenti yüklenemedi ({Path.GetFileName(dll)}): {ex.Message}");
                }
            }
        }

        return new PluginCatalog(preprocessors, postprocessors);
    }
}

public interface IConversionLoggerAdapter
{
    void Warn(string message);
}

public sealed class LoggerAdapter : IConversionLoggerAdapter
{
    private readonly Conversion.IConversionLogger _logger;
    public LoggerAdapter(Conversion.IConversionLogger logger) => _logger = logger;
    public void Warn(string message) => _logger.Warn(message);
}
