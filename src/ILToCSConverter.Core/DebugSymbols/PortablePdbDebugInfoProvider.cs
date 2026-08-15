using System.Reflection.Metadata;
using ICSharpCode.Decompiler.DebugInfo;

namespace ILToCSConverter.Core.DebugSymbols;

internal sealed class PortablePdbDebugInfoProvider : IDebugInfoProvider
{
    private readonly MetadataReaderProvider _provider;
    private readonly string _moduleFileName;
    private readonly string? _pdbFileName;

    public PortablePdbDebugInfoProvider(string moduleFileName, MetadataReaderProvider provider, string? pdbFileName)
    {
        _moduleFileName = moduleFileName;
        _provider = provider;
        _pdbFileName = pdbFileName;
    }

    public string Description => _pdbFileName is null
        ? "Gömülü portable PDB"
        : $"Portable PDB: {_pdbFileName}";

    public string SourceFileName => _pdbFileName ?? _moduleFileName;

    public IList<ICSharpCode.Decompiler.DebugInfo.SequencePoint> GetSequencePoints(MethodDefinitionHandle method)
    {
        var metadata = TryGetReader();
        if (metadata is null)
            return Array.Empty<ICSharpCode.Decompiler.DebugInfo.SequencePoint>();

        try
        {
            var debugInfo = metadata.GetMethodDebugInformation(method);
            var points = new List<ICSharpCode.Decompiler.DebugInfo.SequencePoint>();
            foreach (var point in debugInfo.GetSequencePoints())
            {
                string document = "";
                if (!point.Document.IsNil)
                    document = metadata.GetString(metadata.GetDocument(point.Document).Name);

                points.Add(new ICSharpCode.Decompiler.DebugInfo.SequencePoint
                {
                    Offset = point.Offset,
                    StartLine = point.StartLine,
                    StartColumn = point.StartColumn,
                    EndLine = point.EndLine,
                    EndColumn = point.EndColumn,
                    DocumentUrl = document
                });
            }

            return points;
        }
        catch (BadImageFormatException)
        {
            return Array.Empty<ICSharpCode.Decompiler.DebugInfo.SequencePoint>();
        }
    }

    public IList<Variable> GetVariables(MethodDefinitionHandle method)
    {
        var metadata = TryGetReader();
        var variables = new List<Variable>();
        if (metadata is null)
            return variables;

        foreach (var handle in metadata.GetLocalScopes(method))
        {
            var scope = metadata.GetLocalScope(handle);
            foreach (var variableHandle in scope.GetLocalVariables())
            {
                var variable = metadata.GetLocalVariable(variableHandle);
                variables.Add(new Variable(variable.Index, metadata.GetString(variable.Name)));
            }
        }

        return variables;
    }

    public bool TryGetName(MethodDefinitionHandle method, int index, out string name)
    {
        name = "";
        foreach (var variable in GetVariables(method))
        {
            if (variable.Index == index)
            {
                name = variable.Name;
                return true;
            }
        }

        return false;
    }

    public bool TryGetExtraTypeInfo(MethodDefinitionHandle method, int index, out PdbExtraTypeInfo extraTypeInfo)
    {
        extraTypeInfo = default;
        return false;
    }

    private MetadataReader? TryGetReader()
    {
        try
        {
            return _provider.GetMetadataReader();
        }
        catch (Exception)
        {
            return null;
        }
    }
}
