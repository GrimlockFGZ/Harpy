using HarpyEngine.Resources.Mnemosyne;
using Silk.NET.OpenGL;

namespace HarpyEngine.Rendering.Helios;

public class RenderBridge
{
    private readonly GL _gl;

    public RenderBridge(GL gl)
    {
        _gl = gl;
        
        // Ensure we don't double-subscribe if Bridge is recreated
        ResourceManager.OnShaderRequest -= HandleShaderRequest;
        ResourceManager.OnShaderRequest += HandleShaderRequest;

        ResourceManager.OnReloadRequest -= HandleReloadRequest;
        ResourceManager.OnReloadRequest += HandleReloadRequest;
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