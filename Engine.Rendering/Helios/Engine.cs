using HarpyEngine.Exceptions;
using Silk.NET.Input;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace HarpyEngine.Rendering.Helios;

/// <summary>
/// The core engine class responsible for managing the window, OpenGL context, and the main loop.
/// </summary>
public class Engine
{
    /// <summary>
    /// The window instance used by the engine.
    /// </summary>
    public IWindow EngineWindow { get; }

    /// <summary>
    /// The OpenGL context for rendering.
    /// </summary>
    public GL Gl { get; private set; } = null!;

    /// <summary>
    /// The input context for handling user input.
    /// </summary>
    public IInputContext Input { get; private set; } = null!;

    /// <summary>
    /// Event triggered when the engine has finished loading.
    /// </summary>
    public event Action? OnLoad;

    /// <summary>
    /// Event triggered during the render loop.
    /// </summary>
    public event Action<double>? OnRender;

    /// <summary>
    /// An event triggered during the update phase of the engine's main loop, allowing the execution
    /// of logic that needs to occur at regular intervals such as game state updates, physics calculations,
    /// or input processing.
    /// </summary>
    /// <remarks>
    /// This event provides a time delta as a parameter, representing the time elapsed since the
    /// last update call, making it suitable for time-dependent computations or animations.
    /// </remarks>
    public event Action<double>? OnUpdate;
    
    /// <param name="settings">Optional window settings provider. If null, default settings are used.</param>
    public Engine(WindowSettingsProvider? settings = null)
    {
        // Fallback to default settings if none provided
        settings ??= new WindowSettingsProvider();

        Window.PrioritizeGlfw();
        
        // Grab the pre-configured options from our provider
        var windowOptions = settings.GetOptions();

        EngineWindow = Window.Create(windowOptions);

        EngineWindow.Load += InternalLoad;
        EngineWindow.Resize += (newSize) => 
        {
            Gl.Viewport(0, 0, (uint)newSize.X, (uint)newSize.Y);
        };

        EngineWindow.Render += (d) => OnRender?.Invoke(d);
        EngineWindow.Update += (d) => OnUpdate?.Invoke(d);
        EngineWindow.Closing += InternalClosing;
    }

    /// <summary>
    /// Internal load handler that initializes OpenGL and Input.
    /// </summary>
    private void InternalLoad()
    {
        Gl = EngineWindow.CreateOpenGL();
        Input = EngineWindow.CreateInput(); 

        if (Gl == null) 
            throw new RenderingException("Failed to create OpenGL context. Ensure your GPU drivers are up to date and support OpenGL 4.5+.");

        Gl.Viewport(0, 0, (uint)EngineWindow.Size.X, (uint)EngineWindow.Size.Y);
        OnLoad?.Invoke();
    }

    /// <summary>
    /// Starts the engine's main loop.
    /// </summary>
    public void Run() => EngineWindow.Run();

    /// <summary>
    /// Internal closing handler for resource cleanup.
    /// </summary>
    private void InternalClosing()
    {
        OnLoad = null; 
        Input?.Dispose();
        Gl?.Dispose();
    }
}
