using System.Drawing;
using HarpyEngine.Resources.Mnemosyne;
using Silk.NET.OpenGL;

namespace HarpyEngine.Rendering.Helios;

public class HarpyRenderer(GlContext gl)
{
    private Mesh _triangleMesh = null!;
    private double _totalTime;
    public int TriangleInstanceCount { get; set; }
    private static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
    private readonly string _assetDir = BaseDir + "Assets/";

    public void Init()
    {
        var assetDb = new AssetDatabase();
        assetDb.Init(BaseDir);
        ResourceManager.Init(assetDb);
        
        ResourceManager.OnShaderRequest += (v, f) => new Shader(gl, v, f);
        ResourceManager.OnReloadRequest += (obj) => { if (obj is Shader s) s.Reload(); };
        
        var vPath = Path.Combine(_assetDir, "vertex.glsl"); 
        var fPath = Path.Combine(_assetDir, "fragment.glsl");
        ResourceManager.LoadShader("Default", vPath, fPath); 
        
        float[] vertices = [
            0.0f,  0.2f, 0.0f,
           -0.2f, -0.2f, 0.0f,
            0.2f, -0.2f, 0.0f
        ];
        _triangleMesh = new Mesh(gl, vertices);
    }

    public void Render(double deltaTime, bool isWireframe)
    {
        ResourceManager.CheckForReloads();

        RenderShaders(deltaTime, isWireframe);
    }

    private void RenderShaders(double deltaTime, bool isWireframe)
    {
        gl.Api.ClearColor(Color.FromArgb(30, 30, 35));
        gl.Api.Clear((uint)ClearBufferMask.ColorBufferBit | (uint)ClearBufferMask.DepthBufferBit);

        if (isWireframe)
        {
            gl.Api.PolygonMode(GLEnum.FrontAndBack, GLEnum.Line);
        }

        _totalTime += deltaTime;
        var shader = ResourceManager.Get<Shader>("Default");
        shader.Use();
        
        var program = shader.Handle;
        gl.Api.Uniform1(gl.Api.GetUniformLocation(program, "uGlobalTime"), (float)_totalTime);
        gl.Api.Uniform1(gl.Api.GetUniformLocation(program, "uTime"), (float)_totalTime);
        gl.Api.Uniform1(gl.Api.GetUniformLocation(program, "uInstanceCount"), Math.Max(0, TriangleInstanceCount));
        gl.Api.Uniform1(gl.Api.GetUniformLocation(program, "uRadius"), 0.5f);

        _triangleMesh.DrawInstanced(Math.Max(0, TriangleInstanceCount));
        gl.Api.PolygonMode(GLEnum.FrontAndBack, GLEnum.Fill);
    }
}