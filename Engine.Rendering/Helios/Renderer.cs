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

    public int TriangleInstanceCount { get; set; }
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

        //gl.Api.Enable(EnableCap.DepthTest);
        // gl.Api.Enable(EnableCap.DepthTest);
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
        _blockMeshCache[type] = mesh;
        return mesh;
    }
    private void RenderVoxels(bool isWireframe, float aspectRatio)
    {
        gl.Api.ClearColor(Color.FromArgb(30, 30, 35));
        gl.Api.Clear((uint)ClearBufferMask.ColorBufferBit | (uint)ClearBufferMask.DepthBufferBit);

        var shader = ResourceManager.Get<Shader>("Voxel");
        shader.Use();
    
        // Set view/projection once, as they are constant for the whole frame
        shader.SetMatrix4("uView", Camera.GetViewMatrix());
        shader.SetMatrix4("uProjection", Camera.GetProjectionMatrix(aspectRatio));

        gl.Api.PolygonMode(GLEnum.FrontAndBack, isWireframe ? GLEnum.Line : GLEnum.Fill);

        if (_registry == null) return;

        // This is your single, efficient loop
        _registry.ForEach<Transform, VoxelBlock>(entity =>
        {
            ref var transform = ref _registry.GetComponent<Transform>(entity);
            ref var voxel = ref _registry.GetComponent<VoxelBlock>(entity);

            // Update the Model Matrix for this specific entity
            var model = Matrix4x4.CreateTranslation(transform.Position); 
            shader.SetMatrix4("uModel", model);

            // Draw the mesh
            GetOrBuildMesh(voxel.Type).Draw();
        });
    }
}