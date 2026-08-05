using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Engine.Core;

namespace HarpyEngine.Sandbox.Helpers;

/// <summary>
/// Returns a foreground SolidColorBrush matching each log level's severity color.
/// </summary>
public class LogLevelToBrushConverter : IValueConverter
{
    public static readonly LogLevelToBrushConverter Instance = new();

    private static readonly SolidColorBrush TraceBrush = new(Color.Parse("#555555"));
    private static readonly SolidColorBrush InfoBrush = new(Color.Parse("#999999"));
    private static readonly SolidColorBrush WarningBrush = new(Color.Parse("#f1c40f"));
    private static readonly SolidColorBrush ErrorBrush = new(Color.Parse("#ff7070"));
    private static readonly SolidColorBrush CriticalBrush = new(Color.Parse("#ff3b3b"));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            LogLevel.Trace => TraceBrush,
            LogLevel.Info => InfoBrush,
            LogLevel.Warning => WarningBrush,
            LogLevel.Error => ErrorBrush,
            LogLevel.Critical => CriticalBrush,
            _ => InfoBrush
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException("Converting back from Brush to LogLevel is not supported.");
}