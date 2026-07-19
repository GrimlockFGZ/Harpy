namespace HarpyEngine.Rendering.Voxel;

public static class ChunkMesher
{
    private static readonly (int dx, int dy, int dz)[] Normals =
    [
        ( 1,  0,  0), (-1,  0,  0),
        ( 0,  1,  0), ( 0, -1,  0),
        ( 0,  0,  1), ( 0,  0, -1),
    ];

    public static (float[] Vertices, uint[] Indices) Build(Chunk chunk)
    {
        var verts   = new List<float>();
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
        List<float> verts, List<uint> indices)
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
                var pos  = MakeCoord(d, u, v, slice, i, j);
                var posN = MakeCoord(d, u, v, slice + nx + ny + nz, i, j); 

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
        List<float> verts, List<uint> indices,
        int face, int d, int u, int v,
        int slice, int i, int j, int w, int h,
        int nx, int ny, int nz,
        BlockType type)
    {
        var offset = (nx + ny + nz > 0) ? 1 : 0;

        int[] p0 = MakeCoord(d, u, v, slice + offset, i,     j    );
        int[] p1 = MakeCoord(d, u, v, slice + offset, i + w, j    );
        int[] p2 = MakeCoord(d, u, v, slice + offset, i + w, j + h);
        int[] p3 = MakeCoord(d, u, v, slice + offset, i,     j + h);

        var baseIndex = (uint)(verts.Count / 7);
        
        // FIX: Always append vertices in exact geometric order so indices dictate winding cleanly
        AddVertex(verts, p0, nx, ny, nz, type);
        AddVertex(verts, p1, nx, ny, nz, type);
        AddVertex(verts, p2, nx, ny, nz, type);
        AddVertex(verts, p3, nx, ny, nz, type);

        if (nx + ny + nz > 0)
        {
            // Standard Counter-Clockwise
            indices.Add(baseIndex + 0);
            indices.Add(baseIndex + 1);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 0);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 3);
        }
        else
        {
            // Reversed Clockwise (Flips facing direction for negative axes)
            indices.Add(baseIndex + 0);
            indices.Add(baseIndex + 2);
            indices.Add(baseIndex + 1);
            indices.Add(baseIndex + 0);
            indices.Add(baseIndex + 3);
            indices.Add(baseIndex + 2);
        }
    }

    private static void AddVertex(List<float> verts, int[] p, int nx, int ny, int nz, BlockType type)
    {
        verts.Add(p[0]);
        verts.Add(p[1]);
        verts.Add(p[2]);
        verts.Add(nx);
        verts.Add(ny);
        verts.Add(nz);
        verts.Add((float)type);
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