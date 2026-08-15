namespace ILToCSConverter.Core.Ildasm;

public static class IldasmLocator
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

        yield return Path.Combine(AppContext.BaseDirectory, "tools", "ildasm.exe");

        string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
        string[] sdkRoots =
        [
            Path.Combine(programFilesX86, @"Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8.1 Tools\x64\ildasm.exe"),
            Path.Combine(programFilesX86, @"Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\x64\ildasm.exe"),
            Path.Combine(programFilesX86, @"Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8.1 Tools\ildasm.exe"),
            Path.Combine(programFilesX86, @"Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.8 Tools\ildasm.exe"),
            Path.Combine(programFilesX86, @"Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.7.2 Tools\x64\ildasm.exe"),
            Path.Combine(programFilesX86, @"Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.7.2 Tools\ildasm.exe")
        ];

        foreach (string sdk in sdkRoots)
            yield return sdk;

        string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
        yield return Path.Combine(windows, @"Microsoft.NET\Framework64\v4.0.30319\ildasm.exe");
        yield return Path.Combine(windows, @"Microsoft.NET\Framework\v4.0.30319\ildasm.exe");

        string? pathEnv = Environment.GetEnvironmentVariable("PATH");
        if (string.IsNullOrWhiteSpace(pathEnv))
            yield break;

        foreach (string dir in pathEnv.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            yield return Path.Combine(dir.Trim('"'), "ildasm.exe");
    }
}
