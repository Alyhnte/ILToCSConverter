using System.Windows;
using System.Windows.Media;

namespace ILToCSConverter.App;

public static class ThemeManager
{
    public static void Apply(bool dark)
    {
        var resources = Application.Current.Resources;
        if (dark)
        {
            Set(resources, "Bg", "#0B1220");
            Set(resources, "Card", "#111827");
            Set(resources, "Line", "#1F2937");
            Set(resources, "Text", "#E5E7EB");
            Set(resources, "Muted", "#94A3B8");
            Set(resources, "Accent", "#3B82F6");
            Set(resources, "AccentSoft", "#1E3A5F");
            Set(resources, "Danger", "#F87171");
            Set(resources, "Hover", "#1F2937");
            Set(resources, "LogBg", "#0F172A");
            Set(resources, "CodeBg", "#020617");
        }
        else
        {
            Set(resources, "Bg", "#F3F5F8");
            Set(resources, "Card", "#FFFFFF");
            Set(resources, "Line", "#E2E8F0");
            Set(resources, "Text", "#0F172A");
            Set(resources, "Muted", "#64748B");
            Set(resources, "Accent", "#1D4ED8");
            Set(resources, "AccentSoft", "#DBEAFE");
            Set(resources, "Danger", "#B91C1C");
            Set(resources, "Hover", "#F8FAFC");
            Set(resources, "LogBg", "#F8FAFC");
            Set(resources, "CodeBg", "#0F172A");
        }
    }

    private static void Set(ResourceDictionary resources, string key, string hex)
    {
        resources[key] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!);
    }
}
