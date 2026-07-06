using System;
using System.Diagnostics;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using Engine;
using Engine.Core; // Access our global Event<T> bus
using HarpyEngine.Rendering.Helios;
using HarpyEngine.Sandbox.Editor.Models;
using Silk.NET.OpenGL;

namespace HarpyEngine.Sandbox.Editor;

public class HarpyViewport : OpenGlControlBase
{
    private HarpyRenderer? _renderer;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private double _lastTime;
    
    // Store subscription tokens so we can disconnect safely if the viewport unloads
    private IDisposable? _propChangeSub;
    private IDisposable? _applySub;

    public int TriangleInstanceCount { get; set; }

    public HarpyViewport()
    {
        // Subscribe to our global event channels
        _propChangeSub = Event<PropertyChangedEvent>.Subscribe(OnGlobalPropertyChanged);
        _applySub = Event<ApplyRequestedEvent>.Subscribe(OnGlobalApplyRequested);
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

        _renderer?.TriangleInstanceCount = Math.Max(0, TriangleInstanceCount);

        _renderer?.Render(delta, false);
    
        // Keep rendering loop running smoothly
        Dispatcher.UIThread.Post(RequestNextFrameRendering, DispatcherPriority.Render);
    }

    /// <summary>
    /// Ensure we clean up static event bus bindings when the control detaches from the visual tree
    /// </summary>
    protected override void OnDetachedFromVisualTree(Avalonia.VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        _propChangeSub?.Dispose();
        _applySub?.Dispose();
    }
}