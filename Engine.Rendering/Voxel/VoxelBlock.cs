namespace HarpyEngine.Rendering.Voxel;

/// <summary>
/// Component that marks an entity as a single voxel block to be rendered.
/// </summary>
public struct VoxelBlock
{
    public BlockType Type;

    public VoxelBlock(BlockType type)
    {
        Type = type;
    }
}
