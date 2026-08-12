using HarpyEngine.Resources.Mnemosyne;

namespace HarpyEngine.Rendering.Helios;

public class RenderBridge
{
    private readonly GlContext _gl;
    private readonly List<IDisposable> _subscriptions = [];

    public RenderBridge(GlContext gl)
    {
        _gl = gl;
        
        // Ensure we don't double-subscribe if Bridge is recreated
        ResourceManager.OnShaderRequest -= HandleShaderRequest;
        ResourceManager.OnShaderRequest += HandleShaderRequest;

        _subscriptions.Add(Event<ReloadRequested>.Subscribe(evt => HandleReloadRequest(evt.Resource)));
    }

    private Shader HandleShaderRequest(string vPath, string fPath)
    {
        return new Shader(_gl, vPath, fPath);
    }

    private static void HandleReloadRequest(object asset)
    {
        if (asset is Shader shader)
        {
            shader.Reload();
        }
    }
}