namespace HarpyEngine.Rendering.Voxel;

/// <summary>
/// Identifies the type of a voxel block. Air (0) is the only transparent/empty type.
/// </summary>
public enum BlockType : byte
{
    Air   = 0,
    Stone = 1,
    Dirt  = 2,
    Grass = 3,
    Sand  = 4,
    Wood  = 5,
    Leaf  = 6,
}
