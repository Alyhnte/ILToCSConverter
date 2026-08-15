using ICSharpCode.Decompiler.CSharp;

namespace ILToCSConverter.Core.Conversion;

public static class LanguageVersionMapper
{
    public static readonly IReadOnlyList<string> Choices =
    [
        "CSharp73",
        "CSharp10",
        "CSharp11",
        "CSharp12",
        "Latest"
    ];

    public static LanguageVersion Parse(string? value)
    {
        return (value ?? "CSharp12").Trim().ToLowerInvariant() switch
        {
            "7.3" or "csharp7.3" or "csharp73" or "c#7.3" => LanguageVersion.CSharp7_3,
            "10" or "csharp10" or "c#10" => LanguageVersion.CSharp10_0,
            "11" or "csharp11" or "c#11" => LanguageVersion.CSharp11_0,
            "12" or "csharp12" or "c#12" => LanguageVersion.CSharp12_0,
            "latest" or "en-son" => LanguageVersion.Latest,
            _ => LanguageVersion.CSharp12_0
        };
    }

    public static string DisplayName(string value) => Parse(value) switch
    {
        LanguageVersion.CSharp7_3 => "C# 7.3",
        LanguageVersion.CSharp10_0 => "C# 10",
        LanguageVersion.CSharp11_0 => "C# 11",
        LanguageVersion.CSharp12_0 => "C# 12",
        LanguageVersion.Latest => "En son",
        _ => value
    };
}
