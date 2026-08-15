using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ILToCSConverter.App;

public sealed class InverseBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is false;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is false;
}

public sealed class LevelToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() switch
        {
            "success" => (Brush)new BrushConverter().ConvertFrom("#166534")!,
            "error" => (Brush)new BrushConverter().ConvertFrom("#B91C1C")!,
            "warn" => (Brush)new BrushConverter().ConvertFrom("#B45309")!,
            _ => (Brush)new BrushConverter().ConvertFrom("#334155")!
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        DependencyProperty.UnsetValue;
}
