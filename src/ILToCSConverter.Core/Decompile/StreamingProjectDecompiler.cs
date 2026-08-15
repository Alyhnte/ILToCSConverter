using System.Reflection.Metadata;
using System.Text;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.ProjectDecompiler;
using ICSharpCode.Decompiler.DebugInfo;
using ICSharpCode.Decompiler.Metadata;
using ILToCSConverter.Core.Conversion;

namespace ILToCSConverter.Core.Decompile;

internal sealed class StreamingProjectDecompiler : WholeProjectDecompiler
{
    private readonly ConversionOptions _options;

    public StreamingProjectDecompiler(
        ConversionOptions options,
        DecompilerSettings settings,
        IAssemblyResolver assemblyResolver,
        IDebugInfoProvider? debugInfoProvider)
        : base(settings, assemblyResolver, projectWriter: null, assemblyReferenceClassifier: null, debugInfoProvider)
    {
        _options = options;
    }

    protected override bool IncludeTypeWhenDecompilingProject(MetadataFile module, TypeDefinitionHandle type)
    {
        if (!base.IncludeTypeWhenDecompilingProject(module, type))
            return false;

        var metadata = module.Metadata;
        var typeDef = metadata.GetTypeDefinition(type);
        string name = metadata.GetString(typeDef.Name);
        string ns = metadata.GetString(typeDef.Namespace);

        if (name == "<Module>")
            return false;

        if (string.IsNullOrEmpty(ns) && !_options.IncludeGlobalNamespace)
            return false;

        if (IsCompilerGeneratedName(name) && !_options.IncludeCompilerGenerated)
            return false;

        return true;
    }

    protected override TextWriter CreateFile(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read, 64 * 1024, FileOptions.SequentialScan);
        return new StreamWriter(stream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), 64 * 1024);
    }

    private static bool IsCompilerGeneratedName(string name) =>
        name.StartsWith('<') || name.Contains("DisplayClass", StringComparison.Ordinal) || name.Contains("<>");
}
