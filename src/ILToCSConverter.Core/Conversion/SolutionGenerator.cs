using System.Text;
using ILToCSConverter.Core.Conversion;

namespace ILToCSConverter.Core.Conversion;

public static class SolutionGenerator
{
    public static string? Write(string outputFolder, IReadOnlyList<FileConversionResult> files)
    {
        var projects = files
            .Where(f => f.Status == ConversionStatus.Succeeded && !string.IsNullOrWhiteSpace(f.ProjectFile) && File.Exists(f.ProjectFile))
            .Select(f => f.ProjectFile!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (projects.Length == 0)
            return null;

        string slnName = projects.Length == 1
            ? Path.GetFileNameWithoutExtension(projects[0]) + ".sln"
            : "ILToCS.sln";
        string slnPath = Path.Combine(outputFolder, slnName);

        var builder = new StringBuilder();
        builder.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00");
        builder.AppendLine("# Visual Studio Version 17");
        builder.AppendLine("VisualStudioVersion = 17.0.31903.59");
        builder.AppendLine("MinimumVisualStudioVersion = 10.0.40219.1");

        var guids = new List<(string Name, string Relative, string Guid)>();
        foreach (string project in projects)
        {
            string name = Path.GetFileNameWithoutExtension(project);
            string relative = Path.GetRelativePath(outputFolder, project);
            string guid = Guid.NewGuid().ToString("B").ToUpperInvariant();
            guids.Add((name, relative, guid));
            builder.AppendLine($"Project(\"{{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}}\") = \"{name}\", \"{relative.Replace('\\', '/')}\", \"{guid}\"");
            builder.AppendLine("EndProject");
        }

        builder.AppendLine("Global");
        builder.AppendLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
        builder.AppendLine("\t\tDebug|Any CPU = Debug|Any CPU");
        builder.AppendLine("\t\tRelease|Any CPU = Release|Any CPU");
        builder.AppendLine("\tEndGlobalSection");
        builder.AppendLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
        foreach (var item in guids)
        {
            builder.AppendLine($"\t\t{item.Guid}.Debug|Any CPU.ActiveCfg = Debug|Any CPU");
            builder.AppendLine($"\t\t{item.Guid}.Debug|Any CPU.Build.0 = Debug|Any CPU");
            builder.AppendLine($"\t\t{item.Guid}.Release|Any CPU.ActiveCfg = Release|Any CPU");
            builder.AppendLine($"\t\t{item.Guid}.Release|Any CPU.Build.0 = Release|Any CPU");
        }

        builder.AppendLine("\tEndGlobalSection");
        builder.AppendLine("EndGlobal");

        File.WriteAllText(slnPath, builder.ToString());
        return slnPath;
    }
}
