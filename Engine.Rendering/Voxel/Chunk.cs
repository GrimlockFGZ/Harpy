namespace HarpyEngine.Rendering.Voxel;

/// <summary>
/// A 16×16×16 block of voxel data. Blocks are stored in a flat array indexed
/// by <c>x + Size * (y + Size * z)</c>.
/// </summary>
public sealed class Chunk
{
    public const int Size = 16;
    public const int Volume = Size * Size * Size;

    private readonly BlockType[] _blocks = new BlockType[Volume];

    /// <summary>World-space chunk origin in chunk coordinates (multiply by Size for world units).</summary>
    public (int X, int Y, int Z) ChunkPosition { get; }

    public Chunk(int cx, int cy, int cz)
    {
        ChunkPosition = (cx, cy, cz);
    }

    public BlockType Get(int x, int y, int z) => _blocks[x + Size * (y + Size * z)];

    public void Set(int x, int y, int z, BlockType type) => _blocks[x + Size * (y + Size * z)] = type;

    public bool IsAir(int x, int y, int z) => Get(x, y, z) == BlockType.Air;

    /// <summary>Returns true if the coordinate is inside the chunk bounds.</summary>
    public static bool InBounds(int x, int y, int z) =>
        (uint)x < Size && (uint)y < Size && (uint)z < Size;

    /// <summary>
    /// Fills the chunk with a simple test pattern: solid stone below y=8, grass at y=8, air above.
    /// </summary>
    public void FillTestTerrain()
    {
        for (var z = 0; z < Size; z++)
        for (var y = 0; y < Size; y++)
        for (var x = 0; x < Size; x++)
        {
            if (y < 7)       Set(x, y, z, BlockType.Stone);
            else if (y == 7) Set(x, y, z, BlockType.Dirt);
            else if (y == 8) Set(x, y, z, BlockType.Grass);
            else             Set(x, y, z, BlockType.Air);
        }
    }
}
