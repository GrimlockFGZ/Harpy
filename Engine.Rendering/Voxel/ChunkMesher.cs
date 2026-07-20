using Engine;

namespace HarpyEngine.Rendering.Voxel;

public static class ChunkMesher
{
    private static readonly (int dx, int dy, int dz)[] Normals =
    [
        ( 1,  0,  0), (-1,  0,  0),
        ( 0,  1,  0), ( 0, -1,  0),
        ( 0,  0,  1), ( 0,  0, -1),
    ];

    public static (uint[] Vertices, uint[] Indices) Build(Chunk chunk)
    {
        var verts   = new List<uint>();
        var indices = new List<uint>();

        for (var face = 0; face < 6; face++)
        {
            var (nx, ny, nz) = Normals[face];
            GreedyFace(chunk, face, nx, ny, nz, verts, indices);
        }

        return (verts.ToArray(), indices.ToArray());
    }

    private static void GreedyFace(
        Chunk chunk, int face,
        int nx, int ny, int nz,
        List<uint> verts, List<uint> indices)
    {
        int d, u, v;
        if (nx != 0) { d = 0; u = 1; v = 2; }
        else if (ny != 0) { d = 1; u = 2; v = 0; }
        else              { d = 2; u = 0; v = 1; }

        var size = Chunk.Size;
        var mask = new BlockType[size * size];

        for (var slice = 0; slice < size; slice++)
        {
            for (var j = 0; j < size; j++)
            for (var i = 0; i < size; i++)
            {
                // The current voxel being tested on the slice plane
                var pos  = MakeCoord(d, u, v, slice, i, j);

                var posN = (int[])pos.Clone();
                posN[d] += (nx != 0 ? nx : (ny != 0 ? ny : nz));
                var block = InBounds(pos) ? chunk.Get(pos[0], pos[1], pos[2]) : BlockType.Air;
                var neighbour = InBounds(posN) ? chunk.Get(posN[0], posN[1], posN[2]) : BlockType.Air;

                mask[i + size * j] = (block != BlockType.Air && neighbour == BlockType.Air)
                    ? block
                    : BlockType.Air;
            }

            var used = new bool[size * size];
            for (var j = 0; j < size; j++)
            for (var i = 0; i < size; i++)
            {
                var idx = i + size * j;
                if (used[idx] || mask[idx] == BlockType.Air) continue;

                var type = mask[idx];

                var w = 1;
                while (i + w < size && !used[(i + w) + size * j] && mask[(i + w) + size * j] == type)
                    w++;

                var h = 1;
                while (j + h < size)
                {
                    var rowOk = true;
                    for (var k = 0; k < w; k++)
                    {
                        if (used[(i + k) + size * (j + h)] || mask[(i + k) + size * (j + h)] != type)
                        {
                            rowOk = false;
                            break;
                        }
                    }
                    if (!rowOk) break;
                    h++;
                }

                for (var jj = j; jj < j + h; jj++)
                for (var ii = i; ii < i + w; ii++)
                    used[ii + size * jj] = true;

                EmitQuad(verts, indices, face, d, u, v, slice, i, j, w, h, nx, ny, nz, type);
            }
        }
    }
    
    
    private static void EmitQuad(
        List<uint> verts, List<uint> indices,
        int face, int d, int u, int v,
        int slice, int i, int j, int w, int h,
        int nx, int ny, int nz,
        BlockType type)
    {
        // Determine the axis-specific normal component to place the quad on the correct skin boundary
        int normalComponent = (d == 0) ? nx : (d == 1) ? ny : nz;
        var offset = (normalComponent > 0) ? 1 : 0;

        int[] p0 = MakeCoord(d, u, v, slice + offset, i,     j    );
        int[] p1 = MakeCoord(d, u, v, slice + offset, i + w, j    );
        int[] p2 = MakeCoord(d, u, v, slice + offset, i + w, j + h);
        int[] p3 = MakeCoord(d, u, v, slice + offset, i,     j + h);

        var baseIndex = (uint)verts.Count;

        AddVertex(verts, p0, face, type);
        AddVertex(verts, p1, face, type);
        AddVertex(verts, p2, face, type);
        AddVertex(verts, p3, face, type);

        // Uniform winding correction: positive and negative faces require opposite index orders 
        // to face outward correctly under standard OpenGL backface culling.
        bool isPositive = (normalComponent > 0);

        if (isPositive)
        {
            indices.Add(baseIndex + 0);
            indices.Add(baseIndex + 1);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 0);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 3);
        }
        else
        {
            indices.Add(baseIndex + 0);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 1);
            indices.Add(baseIndex + 0);
            indices.Add(baseIndex + 3);
            indices.Add(baseIndex + 2);
        }
    }
    private static void AddVertex(List<uint> verts, int[] p, int face, BlockType type)
    {
        // TODO: wire up real per-vertex lighting; full-bright (15) for now
        var vertex = new PackedVertex(
            (byte)p[0], (byte)p[1], (byte)p[2],
            (byte)face,
            (ushort)type,
            lightLevel: 15);

        verts.Add(vertex.Raw);
    }

    private static int[] MakeCoord(int d, int u, int v, int dVal, int uVal, int vVal)
    {
        var c = new int[3];
        c[d] = dVal;
        c[u] = uVal;
        c[v] = vVal;
        return c;
    }

    private static bool InBounds(int[] c) => Chunk.InBounds(c[0], c[1], c[2]);
}