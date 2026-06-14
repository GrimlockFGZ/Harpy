using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace HarpyEngine.Sandbox.Helpers;

/// <summary>
/// Returns a short uppercase label string for an asset type.
/// e.g. AssetType.Mesh -> "MESH"
/// </summary>
public class TypeToLabelConverter : IValueConverter
{
    public static readonly TypeToLabelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString()?.ToUpperInvariant() switch
        {
            "MESH"   => "MESH",
            "TEXTURE" or "TEX" => "TEX",
            "MATERIAL" or "MAT" => "MAT",
            "SCENE"  => "SCEN",
            "SCRIPT" or "CS" => "CS",
            "AUDIO"  => "SFX",
            "PREFAB" => "PFB",
            var s    => s?[..Math.Min(4, s?.Length ?? 0)] ?? "?"
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Returns a foreground IBrush matching each asset type's accent color.
/// </summary>
public class TypeToForegroundConverter : IValueConverter
{
    public static readonly TypeToForegroundConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString()?.ToUpperInvariant() switch
        {
            "MESH"              => new SolidColorBrush(Color.Parse("#a48bff")),
            "TEXTURE" or "TEX"  => new SolidColorBrush(Color.Parse("#1ca880")),
            "MATERIAL" or "MAT" => new SolidColorBrush(Color.Parse("#c8871a")),
            "SCENE"             => new SolidColorBrush(Color.Parse("#ff7070")),
            "SCRIPT" or "CS"    => new SolidColorBrush(Color.Parse("#aaaaaa")),
            "AUDIO"             => new SolidColorBrush(Color.Parse("#70d4ff")),
            "PREFAB"            => new SolidColorBrush(Color.Parse("#ff99cc")),
            _                   => new SolidColorBrush(Color.Parse("#666666")),
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}