using Engine.Core;
using HarpyEngine.Rendering.Helios;
using Silk.NET.OpenGL;

namespace HarpyEngine.Rendering.Voxel;

/// <summary>
/// GPU mesh for a greedy-meshed chunk.
/// Vertex layout: 1 packed uint per vertex (see PackedVertex).
/// </summary>
public sealed class VoxelMesh : IDisposable
{
    private readonly GlContext _gl;
    private readonly uint _vao;
    private readonly uint _vbo;
    private readonly uint _ebo;
    private int _indexCount;

    public VoxelMesh(GlContext gl, uint[] vertices, uint[] indices)
    {
        _gl = gl;
        _indexCount = indices.Length;

        _vao = gl.Api.GenVertexArray();
        _vbo = gl.Api.GenBuffer();
        _ebo = gl.Api.GenBuffer();

        gl.Api.BindVertexArray(_vao);

        gl.Api.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        gl.Api.BufferData(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<uint>)vertices, BufferUsageARB.StaticDraw);

        gl.Api.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        gl.Api.BufferData(BufferTargetARB.ElementArrayBuffer, (ReadOnlySpan<uint>)indices, BufferUsageARB.StaticDraw);

        // Single unsigned-int attribute, one component, 4 bytes/vertex.
        // Use VertexAttribIPointer (the "I" matters) so it's read as an
        // integer, not normalized/cast to float.
        gl.Api.VertexAttribIPointer(0, 1, VertexAttribIType.UnsignedInt, sizeof(uint), 0);
        gl.Api.EnableVertexAttribArray(0);

        gl.Api.BindVertexArray(0);
    }

    public void Upload(uint[] vertices, uint[] indices)
    {
        _indexCount = indices.Length;
        _gl.Api.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        _gl.Api.BufferData(BufferTargetARB.ArrayBuffer, (ReadOnlySpan<uint>)vertices, BufferUsageARB.DynamicDraw);
        _gl.Api.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);
        _gl.Api.BufferData(BufferTargetARB.ElementArrayBuffer, (ReadOnlySpan<uint>)indices, BufferUsageARB.DynamicDraw);
    }

    public unsafe void Draw()
    {
        if (_indexCount == 0) { EngineLog.Warning("Draw skipped: indexCount is 0", "VOXEL"); return; }
        _gl.Api.BindVertexArray(_vao);
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