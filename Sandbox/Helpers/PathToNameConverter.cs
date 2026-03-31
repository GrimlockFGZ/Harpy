using System.Globalization;
using Avalonia.Data.Converters;

namespace HarpyEngine.Sandbox.Helpers;

public class PathToNameConverter : IValueConverter
{
    
    public static readonly PathToNameConverter Instance = new();
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string path)
        {
            return Path.GetFileName(path);
        }
        return string.Empty;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value;
    }
       
}