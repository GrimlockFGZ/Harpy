using HarpyEngine.Rendering.Helios;
using Silk.NET.OpenGL;

namespace HarpyEngine.Rendering.Voxel;

/// <summary>
/// GPU mesh for a greedy-meshed chunk.
/// Vertex layout (7 floats per vertex): X Y Z  NX NY NZ  BlockId
/// </summary>
public sealed class VoxelMesh : IDisposable
{
    private readonly GlContext _gl;
    private readonly uint _vao;
    private readonly uint _vbo;
    private readonly uint _ebo;
    private int _indexCount;

    public VoxelMesh(GlContext gl, float[] vertices, uint[] indices)
    {
        _gl = gl;
        _indexCount = indices.Length;

        _vao = gl.Api.GenVertexArray();
        _vbo = gl.Api.GenBuffer();
        _ebo = gl.Api.GenBuffer();

        gl.Api.BindVertexArray(_vao);

        gl.Api.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        gl.Api.BufferData(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)vertices, BufferUsageARB.StaticDraw);

        gl.Api.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        gl.Api.BufferData(BufferTargetARB.ElementArrayBuffer, (ReadOnlySpan<uint>)indices, BufferUsageARB.StaticDraw);

        const uint stride = 7 * sizeof(float);

        // Location 0: position (vec3)
        gl.Api.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, 0);
        gl.Api.EnableVertexAttribArray(0);

        // Location 1: normal (vec3)
        gl.Api.VertexAttribPointer(1, 3, VertexAttribPointerType.Float, false, stride, 3 * sizeof(float));
        gl.Api.EnableVertexAttribArray(1);

        // Location 2: block id (float)
        gl.Api.VertexAttribPointer(2, 1, VertexAttribPointerType.Float, false, stride, 6 * sizeof(float));
        gl.Api.EnableVertexAttribArray(2);

        gl.Api.BindVertexArray(0);
    }

    /// <summary>Re-uploads new geometry without recreating the VAO.</summary>
    public void Upload(float[] vertices, uint[] indices)
    {
        _indexCount = indices.Length;

        _gl.Api.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        _gl.Api.BufferData(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<float>)vertices, BufferUsageARB.DynamicDraw);

        _gl.Api.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        _gl.Api.BufferData(BufferTargetARB.ElementArrayBuffer, (ReadOnlySpan<uint>)indices, BufferUsageARB.DynamicDraw);
    }

    public unsafe void Draw() // Add the 'unsafe' keyword here
    {
        if (_indexCount == 0) return;
        _gl.Api.BindVertexArray(_vao);
        // Now the compiler will allow the (void*) cast
        _gl.Api.DrawElements(PrimitiveType.Triangles, (uint)_indexCount, DrawElementsType.UnsignedInt, (void*)0);
        _gl.Api.BindVertexArray(0);
    }

    public void Dispose()
    {
        _gl.Api.DeleteVertexArray(_vao);
        _gl.Api.DeleteBuffer(_vbo);
        _gl.Api.DeleteBuffer(_ebo);
    }
}
