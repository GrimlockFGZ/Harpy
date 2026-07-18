using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using HarpyEngine.Resources;

namespace HarpyEngine.Sandbox.Helpers;

/// <summary>
/// Converts a <see cref="AssetType"/> enum value into a crisp, vector-based <see cref="StreamGeometry"/> 
/// for rendering high-DPI editor icons via a PathIcon.
/// </summary>
public class AssetTypeToIconConverter : IValueConverter
{
    /// <summary>
    /// Static instance for direct usage or x:Static references within XAML.
    /// </summary>
    public static readonly AssetTypeToIconConverter Instance = new();

    private static readonly Dictionary<AssetType, StreamGeometry> IconMap = new()
    {
        [AssetType.Shader] = StreamGeometry.Parse("M12 22C17.5228 22 22 17.5228 22 12C22 6.47715 17.5228 2 12 2C6.47715 2 2 6.47715 2 12C2 17.5228 6.47715 22 12 22ZM12 17C14.7614 17 17 14.7614 17 12C17 9.23858 14.7614 7 12 7C9.23858 7 7 9.23858 7 12C7 14.7614 9.23858 17 12 17Z"),
        
        [AssetType.Texture] = StreamGeometry.Parse("M3 5C3 3.89543 3.89543 3 5 3H19C20.1046 3 21 3.89543 21 5V19C21 20.1046 20.1046 21 19 21H5C3.89543 21 3 20.1046 3 19V5ZM18 14L14.25 9.5L11.25 13L9 10L6 14H18Z"),
        
        [AssetType.Script] = StreamGeometry.Parse("M14 2H6C4.89543 2 4 2.89543 4 4V20C4 21.1046 4.89543 22 6 22H18C19.1046 22 20 21.1046 20 20V8L14 2ZM9.7 15.7L8.3 14.3L10.6 12L8.3 9.7L9.7 8.3L13.4 12L9.7 15.7ZM14 16H16V14H14V16Z"),
        
        [AssetType.Model] = StreamGeometry.Parse("M12 2.5L3.5 7.5V16.5L12 21.5L20.5 16.5V7.5L12 2.5ZM12 4.7L18.5 8.5L12 12.3L5.5 8.5L12 4.7ZM5.5 10.7L11 13.9V20.1L5.5 16.9V10.7ZM13 20.1V13.9L18.5 10.7V16.9L13 20.1Z"),
        
        [AssetType.Animation] = StreamGeometry.Parse("M4 4H10V10H4V4ZM4 14H10V20H4V14ZM14 4H20V10H14V4ZM14 14H20V20H14V14ZM12 4V20M4 12H20"),
        
        [AssetType.Material] = StreamGeometry.Parse("M12 22C17.5228 22 22 17.5228 22 12C22 6.47715 17.5228 2 12 2C6.47715 2 2 6.47715 2 12C2 17.5228 6.47715 22 12 22ZM13 7H11V13H13V7ZM13 15H11V17H13V15Z"),
        
        [AssetType.Mesh] = StreamGeometry.Parse("M12 2L2 7L12 12L22 7L12 2ZM2 17L12 22L22 17M2 12L12 17L22 12"),
        
        [AssetType.Audio] = StreamGeometry.Parse("M12 3V13.55C11.41 13.21 10.73 13 10 13C7.79 13 6 14.79 6 17C6 19.21 7.79 21 10 21C12.21 21 14 19.21 14 17V7H18V3H12Z"),
        
        [AssetType.Scene] = StreamGeometry.Parse("M19 3H5C3.89 3 3 3.9 3 5V19C3 20.1 3.89 21 5 21H19C20.1 21 21 20.1 21 19V5C21 3.9 20.1 3 19 3ZM19 19H5V5H19V19ZM12 6L7 11H10V16H14V11H17L12 6Z"),
        
        [AssetType.Folder] = StreamGeometry.Parse("M10 4H4C2.9 4 2.01 4.9 2.01 6L2 18C2 19.1 2.9 20 4 20H20C21.1 20 22 19.1 22 18V8C22 6.9 21.1 6 20 6H12L10 4Z"),
        
        [AssetType.Unknown] = StreamGeometry.Parse("M12 2C6.48 2 2 6.48 2 12C2 17.52 6.48 22 12 22C17.52 22 22 17.52 22 12C22 6.48 17.52 2 12 2ZM13 17H11V15H13V17ZM13 13H11V7H13V13Z")
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        switch (value)
        {
            case AssetType assetType when IconMap.TryGetValue(assetType, out var geometry):
                return geometry;
            case string stringType:
            {
                var normalized = stringType.ToUpperInvariant();
                var matchedType = normalized switch
                {
                    "SHADER" => AssetType.Shader,
                    "TEXTURE" or "TEX" => AssetType.Texture,
                    "SCRIPT" or "CS" => AssetType.Script,
                    "MODEL" => AssetType.Model,
                    "ANIMATION" or "ANIM" => AssetType.Animation,
                    "MATERIAL" or "MAT" => AssetType.Material,
                    "MESH" => AssetType.Mesh,
                    "AUDIO" or "SFX" => AssetType.Audio,
                    "SCENE" or "SCEN" => AssetType.Scene,
                    "FOLDER" or "DIR" => AssetType.Folder,
                    _ => AssetType.Unknown
                };

                return IconMap[matchedType];
            }
            default:
                return IconMap[AssetType.Unknown];
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException("Converting from vector stream geometry back to an AssetType is not supported.");
    }
}