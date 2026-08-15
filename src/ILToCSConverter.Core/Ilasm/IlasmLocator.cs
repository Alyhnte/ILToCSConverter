namespace ILToCSConverter.Core.Ilasm;

public static class IlasmLocator
{
    public static string? Find(string? configuredPath = null)
    {
        foreach (string candidate in EnumerateCandidates(configuredPath))
        {
            if (File.Exists(candidate))
                return Path.GetFullPath(candidate);
        }

        return null;
    }

    public static IEnumerable<string> EnumerateCandidates(string? configuredPath = null)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
            yield return configuredPath;

        yield return Path.Combine(AppContext.BaseDirectory, "tools", "ilasm.exe");

        string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        yield return Path.Combine(windows, @"Microsoft.NET\Framework64\v4.0.30319\ilasm.exe");
        yield return Path.Combine(windows, @"Microsoft.NET\Framework\v4.0.30319\ilasm.exe");

        string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        string[] sdkRoots =
        [
            Path.Combine(programFilesX86, @"Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8.1 Tools\ilasm.exe"),
            Path.Combine(programFilesX86, @"Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\ilasm.exe"),
            Path.Combine(programFilesX86, @"Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.7.2 Tools\ilasm.exe")
        ];

        foreach (string sdk in sdkRoots)
            yield return sdk;

        string nuget = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            @".nuget\packages\runtime.win-x64.microsoft.netcore.ilasm");

        if (Directory.Exists(nuget))
        {
            foreach (string found in Directory.EnumerateFiles(nuget, "ilasm.exe", SearchOption.AllDirectories)
                         .OrderByDescending(path => path)
                         .Take(8))
            {
                yield return found;
            }
        }

        string? pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathEnv))
            yield break;

        foreach (string dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            yield return Path.Combine(dir.Trim('"'), "ilasm.exe");
    }
}
