using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using HarpyEngine.Rendering;
using HarpyEngine.Rendering.Helios;

namespace HarpyEngine.Sandbox.Editor;

public class HarpyViewport : OpenGlControlBase
{
    private HarpyRenderer? _renderer;
    private DateTime _lastTime;

    protected override void OnOpenGlInit(GlInterface gl)
    {
        var silkGl = Silk.NET.OpenGL.GL.GetApi(gl.GetProcAddress);
        _renderer = new HarpyRenderer(silkGl);
        _renderer.Init();
        _lastTime = DateTime.UtcNow;
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        var currentTime = DateTime.UtcNow;
        var delta = (currentTime - _lastTime).TotalSeconds;
        _lastTime = currentTime;

        _renderer?.Render(delta, false);

        Dispatcher.UIThread.Post(RequestNextFrameRendering, DispatcherPriority.Render);
    }
}