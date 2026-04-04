using System.Drawing;
using HarpyEngine.Resources.Mnemosyne;
using Silk.NET.OpenGL;

namespace HarpyEngine.Rendering.Helios;

public class HarpyRenderer
{
    private readonly GlContext _gl;
    private Mesh _triangleMesh = null!;
    private double _totalTime;
    private static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
    private readonly string _assetDir = BaseDir + "Assets/";

    public HarpyRenderer(GlContext gl)
    {
        _gl = gl;
    }

    public void Init()
    {
        var assetDb = new AssetDatabase();
        assetDb.Init(BaseDir);
        ResourceManager.Init(assetDb);
        
        ResourceManager.OnShaderRequest += (v, f) => new Shader(_gl, v, f);
        ResourceManager.OnReloadRequest += (obj) => { if (obj is Shader s) s.Reload(); };
        
        var vPath = Path.Combine(_assetDir, "vertex.glsl"); 
        var fPath = Path.Combine(_assetDir, "fragment.glsl");
        ResourceManager.LoadShader("Default", vPath, fPath); 
        
        float[] vertices = [
            0.0f,  0.2f, 0.0f,
           -0.2f, -0.2f, 0.0f,
            0.2f, -0.2f, 0.0f
        ];
        _triangleMesh = new Mesh(_gl, vertices);
    }

    public void Render(double deltaTime, bool isWireframe)
    {
        ResourceManager.CheckForReloads();

        RenderShaders(deltaTime, isWireframe);
    }

    private void RenderShaders(double deltaTime, bool isWireframe)
    {
        _gl.Api.ClearColor(Color.FromArgb(30, 30, 35));
        _gl.Api.Clear((uint)ClearBufferMask.ColorBufferBit | (uint)ClearBufferMask.DepthBufferBit);

        if (isWireframe)
        {
            _gl.Api.PolygonMode(GLEnum.FrontAndBack, GLEnum.Line);
        }

        _totalTime += deltaTime;
        var shader = ResourceManager.Get<Shader>("Default");
        shader.Use();
        
        var program = shader.Handle;
        _gl.Api.Uniform1(_gl.Api.GetUniformLocation(program, "uGlobalTime"), (float)_totalTime);
        
        var radius = 0.5f;
        for (var i = 0; i < 5; i++)
        {
            var angle = i * 2f * (float)Math.PI / 5f;
            var x = (float)Math.Cos(angle) * radius;
            var y = (float)Math.Sin(angle) * radius;
            var loc = _gl.Api.GetUniformLocation(program, $"uOffsets[{i}]");
            _gl.Api.Uniform2(loc, x, y);
        }
        
        _triangleMesh.DrawInstanced(5);
        _gl.Api.PolygonMode(GLEnum.FrontAndBack, GLEnum.Fill);
    }
}