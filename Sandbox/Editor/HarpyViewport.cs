using System.Diagnostics;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using HarpyEngine.Rendering.Helios;

namespace HarpyEngine.Sandbox.Editor;

public class HarpyViewport : OpenGlControlBase
{
    private HarpyRenderer? _renderer;
    private readonly Stopwatch _stopwatch = Stopwatch.StartNew();
    private double _lastTime;

    protected override void OnOpenGlInit(GlInterface gl)
    {
        var silkGl = Silk.NET.OpenGL.GL.GetApi(gl.GetProcAddress);
        var context = new GlContext(silkGl);
        _renderer = new HarpyRenderer(context);
        _renderer.Init();
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        var now = _stopwatch.Elapsed.TotalSeconds;
        var delta = now - _lastTime;
        _lastTime = now;
        _renderer?.Render(delta, false);
        Dispatcher.UIThread.Post(RequestNextFrameRendering, DispatcherPriority.Render);
    }
}