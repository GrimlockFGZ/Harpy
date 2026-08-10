using HarpyEngine.Rendering.Helios;
using Silk.NET.OpenGL;

namespace HarpyEngine.Rendering;

/// <summary>
/// Represents a 3D mesh consisting of vertices.
/// </summary>
public class Mesh
{
    private readonly uint _vao;
    private readonly GlContext _gl;
    private readonly int _vertexCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="Mesh"/> class.
    /// </summary>
    /// <param name="gl">The OpenGL context.</param>
    /// <param name="vertices">The array of vertex data.</param>
    public Mesh(GlContext gl, float[] vertices)
    {
        _gl = gl; 
        _vertexCount = vertices.Length / 3;
        
        _vao = _gl.Api.GenVertexArray(); 
        var vbo = _gl.Api.GenBuffer();
        
        _gl.Api.BindVertexArray(_vao); 
        _gl.Api.BindBuffer(BufferTargetARB.ArrayBuffer, vbo);
        
        _gl.Api.BufferData(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)vertices, BufferUsageARB.StaticDraw);
        
        // Position attribute (Location 0) 
        _gl.Api.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, 3 * sizeof(float), 0); 
        _gl.Api.EnableVertexAttribArray(0);
    }

    /// <summary>
    /// Draws the mesh using instanced rendering.
    /// </summary>
    /// <param name="instanceCount">The number of instances to draw.</param>
    public void DrawInstanced(int instanceCount)
    {
        _gl.Api.BindVertexArray(_vao);
        _gl.Api.DrawArraysInstanced(PrimitiveType.Triangles, 0, (uint)_vertexCount, (uint)instanceCount);
    }
}