using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using HarpyEngine.Resources;

namespace HarpyEngine.Sandbox.Helpers;

/// <summary>
/// Returns a foreground SolidColorBrush matching each asset type's professional engine accent color.
/// Supports both enum evaluations and string fallbacks.
/// </summary>
public class TypeToForegroundConverter : IValueConverter
{
    public static readonly TypeToForegroundConverter Instance = new();

    private static readonly SolidColorBrush ShaderBrush = new(Color.Parse("#ff6b6b"));    // Warm Red/Orange
    private static readonly SolidColorBrush TextureBrush = new(Color.Parse("#1ca880"));   // Mint Green
    private static readonly SolidColorBrush ScriptBrush = new(Color.Parse("#f1c40f"));    // Gold/Yellow
    private static readonly SolidColorBrush ModelBrush = new(Color.Parse("#5bc0de"));     // Cyan Blue
    private static readonly SolidColorBrush AnimationBrush = new(Color.Parse("#e84393")); // Magenta/Pink
    private static readonly SolidColorBrush MaterialBrush = new(Color.Parse("#d35400"));  // Deep Amber
    private static readonly SolidColorBrush MeshBrush = new(Color.Parse("#a48bff"));      // Soft Purple
    private static readonly SolidColorBrush AudioBrush = new(Color.Parse("#70d4ff"));      // Sky Blue
    private static readonly SolidColorBrush SceneBrush = new(Color.Parse("#ff7070"));      // Coral Red
    private static readonly SolidColorBrush FolderBrush = new(Color.Parse("#f39c12"));     // Folder Orange
    private static readonly SolidColorBrush DefaultBrush = new(Color.Parse("#aaaaaa"));    // Muted Gray

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            AssetType type => type switch
            {
                AssetType.Shader => ShaderBrush,
                AssetType.Texture => TextureBrush,
                AssetType.Script => ScriptBrush,
                AssetType.Model => ModelBrush,
                AssetType.Animation => AnimationBrush,
                AssetType.Material => MaterialBrush,
                AssetType.Mesh => MeshBrush,
                AssetType.Audio => AudioBrush,
                AssetType.Scene => SceneBrush,
                AssetType.Folder => FolderBrush,
                _ => DefaultBrush
            },
            string stringType => stringType.ToUpperInvariant() switch
            {
                "SHADER" => ShaderBrush,
                "TEXTURE" or "TEX" => TextureBrush,
                "SCRIPT" or "CS" => ScriptBrush,
                "MODEL" => ModelBrush,
                "ANIMATION" or "ANIM" => AnimationBrush,
                "MATERIAL" or "MAT" => MaterialBrush,
                "MESH" => MeshBrush,
                "AUDIO" or "SFX" => AudioBrush,
                "SCENE" or "SCEN" => SceneBrush,
                "FOLDER" or "DIR" => FolderBrush,
                _ => DefaultBrush
            },
            _ => DefaultBrush
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException("Converting back from Brush to AssetType is not supported.");
}