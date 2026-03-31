using Silk.NET.Maths;
using Silk.NET.Windowing;

namespace HarpyEngine.Rendering.Helios;

/// <summary>
/// Provides configurable settings for creating a Silk.NET window used by the engine.
/// </summary>
public class WindowSettingsProvider
{
    /// <summary>
    /// The window title.
    /// </summary>´
    public string Title { get; set; } = "Harpy Engine";

    /// <summary>
    /// The desired window width in pixels.
    /// </summary>
    public int Width { get; set; } = 1920;

    /// <summary>
    /// The desired window height in pixels.
    /// </summary>
    public int Height { get; set; } = 1080;

    /// <summary>
    /// The initial window state (e.g., Normal, Fullscreen).
    /// </summary>
    public WindowState State { get; set; } = WindowState.Fullscreen;

    /// <summary>
    /// Generates a Silk.NET <see cref="WindowOptions"/> object based on current settings.
    /// </summary>
    /// <returns>A configured <see cref="WindowOptions"/> instance.</returns>
    public WindowOptions GetOptions()
    {
        var options = WindowOptions.Default;
        options.Title = Title;
        options.Size = new Vector2D<int>(Width, Height);
        options.WindowState = State;
        
        // Graphics API Setup
        options.API = new GraphicsAPI(
            ContextAPI.OpenGL, 
            ContextProfile.Core, 
            ContextFlags.Default, 
            new APIVersion(3, 3)
        );
        
        options.ShouldSwapAutomatically = true;
        
        return options;
    }
}