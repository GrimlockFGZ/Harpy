using System.Drawing;
using Engine;
using Engine.Core;
using HarpyEngine.Rendering.Voxel;
using HarpyEngine.Resources.Mnemosyne;
using Silk.NET.OpenGL;

namespace HarpyEngine.Rendering.Helios;

public class HarpyRenderer(GlContext gl)
{
    private readonly List<IDisposable> _subscriptions = [];
    private readonly Dictionary<BlockType, VoxelMesh> _blockMeshCache = new();
    private Registry? _registry;

    private static readonly string BaseDir = AppDomain.CurrentDomain.BaseDirectory;
    private readonly string _assetDir = BaseDir + "Assets/";

    public Camera Camera { get; } = new(new Vector3(4f, 2f, 12f));

    public void SetRegistry(Registry registry) => _registry = registry;

    public void Init()
    {
        var assetDb = AssetDatabase.Instance;
        assetDb.Init(BaseDir);
        ResourceManager.Init(assetDb);
        ResourceManager.OnShaderRequest += (v, f) => new Shader(gl, v, f);
        _subscriptions.Add(Event<ReloadRequested>.Subscribe(evt => { if (evt.Resource is Shader s) s.Reload(); }));
        var vPath = Path.Combine(_assetDir, "voxel_vertex.glsl");
        var fPath = Path.Combine(_assetDir, "voxel_fragment.glsl");
        ResourceManager.LoadShader("Voxel", vPath, fPath);

        gl.Api.Disable(EnableCap.DepthTest);
        gl.Api.Enable(EnableCap.CullFace);
        gl.Api.CullFace(TriangleFace.Back);
    }

    public void Render(double deltaTime, bool isWireframe, float aspectRatio)
    {
        ResourceManager.CheckForReloads();
        RenderVoxels(isWireframe, aspectRatio);
    }

    private VoxelMesh GetOrBuildMesh(BlockType type)
    {
        if (_blockMeshCache.TryGetValue(type, out var cached))
            return cached;

        var chunk = new Chunk(0, 0, 0);
        chunk.Set(0, 0, 0, type);
        var (verts, indices) = ChunkMesher.Build(chunk);

        var mesh = new VoxelMesh(gl, verts, indices);
        mesh.Upload(verts, indices);

        _blockMeshCache[type] = mesh;
        return mesh;
    }
    private unsafe void RenderVoxels(bool isWireframe, float aspectRatio)
    {
        gl.Api.ClearColor(Color.FromArgb(30, 30, 35));
        gl.Api.Clear((uint)ClearBufferMask.ColorBufferBit | (uint)ClearBufferMask.DepthBufferBit);

        var shader = ResourceManager.Get<Shader>("Voxel");
        shader.Use();

        shader.SetMatrix4("uView", Camera.GetViewMatrix());
        shader.SetMatrix4("uProjection", Camera.GetProjectionMatrix(aspectRatio));
        gl.Api.PolygonMode(GLEnum.FrontAndBack, isWireframe ? GLEnum.Line : GLEnum.Fill);

        // --- EXACT CULLING & DEPTH STATE FOR 3D ---
        gl.Api.Enable(EnableCap.DepthTest); // MUST be on so front blocks hide back blocks
        gl.Api.Enable(EnableCap.CullFace);  // MUST be on for performance/solid looking meshes
        gl.Api.CullFace(TriangleFace.Back); 
        gl.Api.FrontFace(FrontFaceDirection.Ccw); // Explicitly state winding expectation

        if (_registry == null) return;

        _registry.ForEach<Transform, VoxelBlock>(entity =>
        {
            ref var transform = ref _registry.GetComponent<Transform>(entity);
            ref var voxel = ref _registry.GetComponent<VoxelBlock>(entity);
            var model = Matrix4x4.CreateTranslation(transform.Position);
            shader.SetMatrix4("uModel", model);
            GetOrBuildMesh(voxel.Type).Draw();
        });

        var p = stackalloc byte[4];
        gl.Api.ReadPixels(392, 217, 1, 1, PixelFormat.Rgba, PixelType.UnsignedByte, p);
        
    }
}