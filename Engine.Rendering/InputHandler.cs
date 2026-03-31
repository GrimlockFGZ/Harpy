using HarpyEngine.Rendering.Helios;
using Silk.NET.Input;
using Silk.NET.Windowing;

namespace HarpyEngine.Rendering;

/// <summary>
/// Handles user input by mapping keys to specific actions within the engine.
/// </summary>
public class InputHandler
{
    public bool IsWireframe { get; private set; }

    /// <summary>
    /// A collection of keyboard hotkey mappings to specified actions.
    /// Each hotkey, represented by a <see cref="Silk.NET.Input.Key"/>, is mapped to a corresponding action,
    /// which is invoked when the hotkey is activated. These actions are specific to engine behavior or functionality.
    /// </summary>
    /// <remarks>
    /// The dictionary maps <see cref="Silk.NET.Input.Key"/> objects to <see cref="System.Action{T1, T2}"/> delegates,
    /// where the first parameter is the <see cref="Engine"/> instance and the second is the <see cref="Silk.NET.Input.IKeyboard"/> instance.
    /// Also used internally to define and handle default key bindings such as toggling wireframe mode, VSync, fullscreen, and cursor behavior.
    /// </remarks>
    private readonly Dictionary<Key, Action<Helios.Engine, IKeyboard>> _hotkeys = new();

    /// <summary>
    /// Initializes the input handler by registering default key bindings and setting up
    /// event listeners for input devices connected to the engine.
    /// </summary>
    /// <param name="engine">The engine instance that provides input context and devices.</param>
    public void Initialize(Helios.Engine engine)
    {
        RegisterDefaultBindings();

        foreach (var keyboard in engine.Input.Keyboards)
        {
            keyboard.KeyDown += (kb, key, code) => 
            {
                HandleKeyDown(engine, kb, key);
            };
        }
        
        engine.Input.ConnectionChanged += (device, connected) =>
        {
            if (connected && device is IKeyboard keyboard)
            {
                keyboard.KeyDown += (kb, key, code) => HandleKeyDown(engine, kb, key);
            }
        };
    }

    /// Registers the default key bindings for the application.
    /// This method initializes a predefined set of key bindings that map specific keyboard keys
    /// to corresponding actions in the application. These actions include toggling wireframe mode,
    /// toggling fullscreen mode, switching cursor modes, enabling/disabling VSync, and closing the application.
    /// Each action is executed when the associated key is pressed, with optional modifier keys like "Alt" being considered.
    /// The key bindings are stored internally in a dictionary, where the key is a `Key` enum value
    /// representing the keyboard key, and the value is an `Action` delegate that performs the associated
    /// behavior given access to the `Engine` instance and the `IKeyboard` input context.
    /// Preconditions:
    /// - An `Engine` instance and its input system must be initialized before invoking these bindings.
    /// Side Effects:
    /// - Modifies the application's behavior in response to key events.
    /// - Alters rendering settings, window state, cursor visibility, and input modes based on user interaction.
    private void RegisterDefaultBindings()
    {
        _hotkeys[Key.F4] = (eng, kb) => {
            if (IsAltPressed(kb)) eng.EngineWindow.Close();
        };

        _hotkeys[Key.Space] = (eng, kb) => {
            IsWireframe = !IsWireframe;
        };

        _hotkeys[Key.Enter] = (eng, kb) => {
            if (IsAltPressed(kb)) 
            {
                eng.EngineWindow.WindowState = eng.EngineWindow.WindowState == WindowState.Fullscreen 
                    ? WindowState.Normal : WindowState.Fullscreen;
            }
        };

        _hotkeys[Key.Escape] = (eng, kb) =>
        {
            if (eng.Input.Mice.Count <= 0) return;
            var mouse = eng.Input.Mice[0];
            mouse.Cursor.CursorMode = mouse.Cursor.CursorMode == CursorMode.Raw 
                ? CursorMode.Normal : CursorMode.Raw;
        };

        _hotkeys[Key.F2] = (eng, kb) => {
            eng.EngineWindow.VSync = !eng.EngineWindow.VSync;
        };
    }

    /// <summary>
    /// Handles the action that should be performed when a key is pressed on the keyboard.
    /// </summary>
    /// <param name="engine">The engine instance managing the current game or application state.</param>
    /// <param name="keyboard">The keyboard device from which the key press event originated.</param>
    /// <param name="key">The key that was pressed.</param>
    private void HandleKeyDown(Helios.Engine engine, IKeyboard keyboard, Key key)
    {
        if (_hotkeys.TryGetValue(key, out var action))
        {
            action.Invoke(engine, keyboard);
        }
    }

    /// <summary>
    /// Checks if either the left or right Alt key is currently pressed.
    /// </summary>
    /// <param name="kb">The keyboard device to check.</param>
    /// <returns>True if either Alt key is pressed; otherwise, false.</returns>
    private static bool IsAltPressed(IKeyboard kb) => kb.IsKeyPressed(Key.AltLeft) || kb.IsKeyPressed(Key.AltRight);
}