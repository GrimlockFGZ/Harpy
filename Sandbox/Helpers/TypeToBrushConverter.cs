using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using HarpyEngine.Resources.Mnemosyne;

namespace HarpyEngine.Sandbox.Helpers;

public class TypeToBrushConverter : IValueConverter
{
    public TypeToBrushConverter() { }

    public static readonly TypeToBrushConverter Instance = new();

    private static readonly Dictionary<AssetDatabase.AssetType, IBrush> BrushMap = new()
    {
        { AssetDatabase.AssetType.Shader, Brushes.Brown },
        { AssetDatabase.AssetType.Texture, Brushes.DarkOliveGreen },
        { AssetDatabase.AssetType.Model, Brushes.SteelBlue },
        { AssetDatabase.AssetType.Script, Brushes.Gold }
    };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is AssetDatabase.AssetType type && BrushMap.TryGetValue(type, out var brush))
            return brush;
        return Brushes.DimGray;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) 
        => throw new NotSupportedException();
}