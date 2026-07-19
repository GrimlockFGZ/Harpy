using System.Diagnostics;
using Avalonia;
using Avalonia.Input;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Rendering;
using Avalonia.Threading;
using Avalonia.Controls;
using Engine;
using Engine.Core; 
using HarpyEngine.Rendering.Helios;
using HarpyEngine.Sandbox.Editor.Models;
using Silk.NET.OpenGL;

namespace HarpyEngine.Sandbox.Editor;

public class HarpyViewport : OpenGlControlBase, ICustomHitTest
{
    private HarpyRenderer? _renderer;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private double _lastTime;

    private const float MoveSpeed = 3f; 
    private const float LookSensitivity = 0.005f; 
    private readonly Lock _inputLock = new();
    private bool _isLooking;
    private Point _lastPointerPosition;
    private float _pendingYawDelta;
    private float _pendingPitchDelta;
    private readonly HashSet<Key> _keysDown = [];

    private IDisposable? _propChangeSub;
    private IDisposable? _applySub;

    public int TriangleInstanceCount { get; set; }

    private Registry? _pendingRegistry;

    public void SetRegistry(Registry registry)
    {
        _pendingRegistry = registry;
        if (_renderer is not null)
            _renderer.SetRegistry(registry);
    }

    public HarpyViewport()
    {
        Focusable = true;
        _propChangeSub = Event<PropertyChangedEvent>.Subscribe(OnGlobalPropertyChanged);
        _applySub = Event<ApplyRequestedEvent>.Subscribe(OnGlobalApplyRequested);
    }

    bool ICustomHitTest.HitTest(Point point) => Bounds.WithX(0).WithY(0).Contains(point);

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();

        if (e.GetCurrentPoint(this).Properties.IsRightButtonPressed)
        {
            lock (_inputLock)
            {
                _isLooking = true;
                _lastPointerPosition = e.GetPosition(this);
            }
            e.Pointer.Capture(this);
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (e.InitialPressMouseButton == MouseButton.Right)
        {
            lock (_inputLock) { _isLooking = false; }
            e.Pointer.Capture(null);
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        lock (_inputLock)
        {
            if (!_isLooking) return;

            var current = e.GetPosition(this);
            var deltaX = (float)(current.X - _lastPointerPosition.X);
            var deltaY = (float)(current.Y - _lastPointerPosition.Y);
            _lastPointerPosition = current;

            _pendingYawDelta += deltaX * LookSensitivity;
            _pendingPitchDelta += -deltaY * LookSensitivity;
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        base.OnKeyDown(e);
        lock (_inputLock) { _keysDown.Add(e.Key); }
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        base.OnKeyUp(e);
        lock (_inputLock) { _keysDown.Remove(e.Key); }
    }

    private void ApplyCameraMovement(float deltaTime)
    {
        if (_renderer is null) return;

        Key[] keysSnapshot;
        float yawDelta, pitchDelta;
        lock (_inputLock)
        {
            keysSnapshot = _keysDown.Count > 0 ? [.. _keysDown] : [];
            yawDelta = _pendingYawDelta;
            pitchDelta = _pendingPitchDelta;
            _pendingYawDelta = 0f;
            _pendingPitchDelta = 0f;
        }

        var camera = _renderer.Camera;

        if (yawDelta != 0f || pitchDelta != 0f)
        {
            camera.AddYawPitch(yawDelta, pitchDelta);
        }

        if (keysSnapshot.Length == 0) return;

        var move = Vector3.Zero;
        foreach (var key in keysSnapshot)
        {
            move += key switch
            {
                Key.W => camera.Forward,
                Key.S => -camera.Forward,
                Key.D => camera.Right,
                Key.A => -camera.Right,
                Key.E => Vector3.Up,
                Key.Q => -Vector3.Up,
                _ => Vector3.Zero
            };
        }

        if (move == Vector3.Zero) return;

        camera.Position += move.Normalized() * (MoveSpeed * deltaTime);
    }

    private void OnGlobalPropertyChanged(PropertyChangedEvent evt)
    {
        if (evt.Sender is InspectorViewModel && 
            (evt.PropertyName.StartsWith("Position") || evt.PropertyName.StartsWith("Scale")))
        {
            Dispatcher.UIThread.Post(RequestNextFrameRendering, DispatcherPriority.Render);
        }
    }

    private void OnGlobalApplyRequested(ApplyRequestedEvent evt)
    {
        Logger.LogInfo("Global Apply Requested:");
    }

    protected override void OnOpenGlInit(GlInterface gl)
    {
        try
        {
            var silkGl = GL.GetApi(gl.GetProcAddress);
            var context = new GlContext(silkGl);
            _renderer = new HarpyRenderer(context);
        
            // If anything in here throws (like file not found for shaders), we catch it
            _renderer.Init(); 
        
            if (_pendingRegistry is not null)
                _renderer.SetRegistry(_pendingRegistry);
            
            Console.WriteLine("[OPENGL] Initialization Successful!");
        }
        catch (Exception ex)
        {
            // Put a breakpoint here! 
            Console.WriteLine($"[CRITICAL] OpenGL Init Crashed: {ex.Message}\n{ex.StackTrace}");
            Debug.WriteLine($"[CRITICAL] OpenGL Init Crashed: {ex.Message}");
        }
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        var silkGl = GL.GetApi(gl.GetProcAddress);
    
        // 1. Bind the buffer and set the viewport using the SCALED dimensions
        silkGl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)fb);
    
        var scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
        var fbWidth = (uint)(Bounds.Width * scaling);
        var fbHeight = (uint)(Bounds.Height * scaling);
        silkGl.Viewport(0, 0, fbWidth, fbHeight);

        // 2. Clear ONCE
        silkGl.ClearColor(1.0f, 0.0f, 1.0f, 1.0f); // Hot Pink
        silkGl.Clear((uint)ClearBufferMask.ColorBufferBit);

        // 3. Update time and logic
        var now = _stopwatch.Elapsed.TotalSeconds;
        var delta = now - _lastTime;
        _lastTime = now;
        ApplyCameraMovement((float)delta);

        // 4. Render
        if (_renderer != null)
        {
            _renderer.TriangleInstanceCount = Math.Max(0, TriangleInstanceCount);
            var aspectRatio = fbHeight > 0 ? (float)fbWidth / fbHeight : 1f;
        
            // Ensure this method does NOT call glClear again!
            _renderer.Render(delta, false, aspectRatio);
        }
    
        // 5. Diagnostics
        var error = silkGl.GetError();
        if (error != GLEnum.NoError)
        {
            Console.WriteLine($"[GL ERROR] {error}");
        }
        // Keep the loop going
        Dispatcher.UIThread.Post(RequestNextFrameRendering, DispatcherPriority.Render);
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _propChangeSub?.Dispose();
        _applySub?.Dispose();
    }
}