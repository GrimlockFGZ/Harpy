using System.Diagnostics;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using HarpyEngine.Rendering.Helios;
using Silk.NET.OpenGL;

namespace HarpyEngine.Sandbox.Editor;

public class HarpyViewport : OpenGlControlBase
{
    private HarpyRenderer? _renderer;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private double _lastTime;

    public int TriangleInstanceCount { get; set; }

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

        if (_renderer != null)
        {
            _renderer.TriangleInstanceCount = Math.Max(0, TriangleInstanceCount);
        }

        _renderer?.Render(delta, false);
    
        Dispatcher.UIThread.Post(RequestNextFrameRendering, DispatcherPriority.Render);
    }
}