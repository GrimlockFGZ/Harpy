using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Avalonia;
using Avalonia.Input;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Rendering;
using Avalonia.Threading;
using Engine;
using Engine.Core; // Access our global Event<T> bus
using HarpyEngine.Rendering.Helios;
using HarpyEngine.Sandbox.Editor.Models;
using Silk.NET.OpenGL;

namespace HarpyEngine.Sandbox.Editor;

public class HarpyViewport : OpenGlControlBase, ICustomHitTest
{
    private HarpyRenderer? _renderer;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private double _lastTime;

    // --- Camera control state ---
    // NOTE: pointer/keyboard callbacks fire on the UI thread, but OnOpenGlRender runs on
    // a dedicated render thread (Avalonia does not synchronize the two), so all shared
    // input state below is guarded by _inputLock rather than touched directly.
    private const float MoveSpeed = 3f; // world units / second
    private const float LookSensitivity = 0.005f; // radians / pixel
    private readonly Lock _inputLock = new();
    private bool _isLooking;
    private Point _lastPointerPosition;
    private float _pendingYawDelta;
    private float _pendingPitchDelta;
    private readonly HashSet<Key> _keysDown = [];

    // Store subscription tokens so we can disconnect safely if the viewport unloads
    private IDisposable? _propChangeSub;
    private IDisposable? _applySub;

    public int TriangleInstanceCount { get; set; }

    public HarpyViewport()
    {
        Focusable = true;

        // Subscribe to our global event channels
        _propChangeSub = Event<PropertyChangedEvent>.Subscribe(OnGlobalPropertyChanged);
        _applySub = Event<ApplyRequestedEvent>.Subscribe(OnGlobalApplyRequested);
    }

    /// <summary>
    /// Ensures Avalonia routes pointer events to this control; OpenGlControlBase's
    /// default hit-testing can otherwise miss the surface. See AvaloniaUI/Avalonia#10812.
    /// </summary>
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

            // Accumulate; the render thread consumes and clears this each frame.
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

    /// <summary>
    /// Applies accumulated look input and WASD/Q/E movement relative to the camera's
    /// current facing. Called once per rendered frame (on the render thread) so movement
    /// speed is frame-rate independent. All camera mutation happens here, and only here,
    /// so the Camera itself never needs its own locking.
    /// </summary>
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
        // If the inspector modifies any coordinate data, request a new OpenGL frame
        if (evt.Sender is InspectorViewModel && 
            (evt.PropertyName.StartsWith("Position") || evt.PropertyName.StartsWith("Scale")))
        {
            // Forces Avalonia's render loop to call OnOpenGlRender on the UI thread
            Dispatcher.UIThread.Post(RequestNextFrameRendering, DispatcherPriority.Render);
        }
    }

    private void OnGlobalApplyRequested(ApplyRequestedEvent evt)
    {
        Logger.LogInfo("Global Apply Requested:");
    }

    protected override void OnOpenGlInit(GlInterface gl)
    {
        var silkGl = GL.GetApi(gl.GetProcAddress);
        var context = new GlContext(silkGl);
        _renderer = new HarpyRenderer(context);
        _renderer.Init();
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        var silkGl = GL.GetApi(gl.GetProcAddress);
        silkGl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)fb);

        silkGl.Viewport(0, 0, (uint)Bounds.Width, (uint)Bounds.Height);

        var now = _stopwatch.Elapsed.TotalSeconds;
        var delta = now - _lastTime;
        _lastTime = now;

        ApplyCameraMovement((float)delta);

        _renderer?.TriangleInstanceCount = Math.Max(0, TriangleInstanceCount);

        var aspectRatio = Bounds.Height > 0 ? (float)(Bounds.Width / Bounds.Height) : 1f;
        _renderer?.Render(delta, false, aspectRatio);
    
        // Keep rendering loop running smoothly
        Dispatcher.UIThread.Post(RequestNextFrameRendering, DispatcherPriority.Render);
    }

    /// <summary>
    /// Ensure we clean up static event bus bindings when the control detaches from the visual tree
    /// </summary>
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _propChangeSub?.Dispose();
        _applySub?.Dispose();
    }
}