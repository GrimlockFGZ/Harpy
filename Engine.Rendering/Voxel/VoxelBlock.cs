namespace HarpyEngine.Rendering.Voxel;

/// <summary>
/// Component that marks an entity as a single voxel block to be rendered.
/// </summary>
public struct VoxelBlock(BlockType type)
{
    public BlockType Type = type;
}
