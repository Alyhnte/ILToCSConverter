using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;
using ILToCSConverter.Core.Conversion;

namespace ILToCSConverter.Core.Decompile;

public sealed class TypePreviewItem
{
    public required string FullName { get; init; }
    public required string Name { get; init; }
    public required string Namespace { get; init; }
    public override string ToString() => string.IsNullOrEmpty(Namespace) ? Name : $"{Namespace}.{Name}";
}

public static class TypePreviewService
{
    public static IReadOnlyList<TypePreviewItem> ListTypes(string assemblyPath)
    {
        var settings = new DecompilerSettings(LanguageVersion.Latest) { ThrowOnAssemblyResolveErrors = false };
        var decompiler = new CSharpDecompiler(assemblyPath, settings);
        return decompiler.TypeSystem.MainModule.TypeDefinitions
            .Where(type => type.Kind != TypeKind.Delegate)
            .Where(type => !string.IsNullOrEmpty(type.Name) && !type.Name.StartsWith('<'))
            .Select(type => new TypePreviewItem
            {
                FullName = type.FullName,
                Name = type.Name,
                Namespace = type.Namespace ?? ""
            })
            .OrderBy(type => type.Namespace, StringComparer.OrdinalIgnoreCase)
            .ThenBy(type => type.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static string DecompileType(string assemblyPath, string fullName, string languageVersion = "CSharp12")
    {
        var settings = new DecompilerSettings(LanguageVersionMapper.Parse(languageVersion))
        {
            ThrowOnAssemblyResolveErrors = false
        };
        var decompiler = new CSharpDecompiler(assemblyPath, settings);
        return decompiler.DecompileTypeAsString(new FullTypeName(fullName));
    }
}
