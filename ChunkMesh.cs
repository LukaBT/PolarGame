public class ChunkMesh
{
    private World world;

    // Same for every chunk
    public ushort[] indices = null!;

    // Same for every cartesian chunk
    public float[] cartesianMesh = null!;

    // Polar meshes made so far, key is band and chunkRow packed together
    public Dictionary<long, float[]> polarMeshes = new Dictionary<long, float[]>();

    public ChunkMesh(World world)
    {
        this.world = world;

        BuildIndices();
        BuildCartesianMesh();
    }

    private void BuildIndices()
    {
        int n = world.chunkSize + 1;

        // For every block in chunk there is two triangles with three vertices each(2*3=6)
        indices = new ushort[world.chunkSize * world.chunkSize * 6];

        int k = 0;
        for (int j = 0; j < world.chunkSize; j++)
        {
            for (int i = 0; i < world.chunkSize; i++)
            {
                ushort a = (ushort)(j * n + i);
                ushort b = (ushort)(a + 1);
                ushort c = (ushort)(a + n);
                ushort d = (ushort)(c + 1);

                indices[k++] = a;
                indices[k++] = c;
                indices[k++] = b;

                indices[k++] = b;
                indices[k++] = c;
                indices[k++] = d;
            }
        }
    }

    // Vertices are local to the bottom left corner of the chunk, same as polar meshes
    private void BuildCartesianMesh()
    {
        int n = world.chunkSize + 1;
        cartesianMesh = new float[n * n * 2];

        for (int j = 0; j < n; j++)
        {
            for (int i = 0; i < n; i++)
            {
                int index = (j * n + i) * 2;

                cartesianMesh[index] = i;
                cartesianMesh[index + 1] = j;
            }
        }
    }


    // Same mesh for every chunk in this row of the band, vertices are local to the chunk corner
    public float[] GetPolarMesh(Band band, int chunkRow)
    {
        long key = ((long)band.band << 32) | (uint)chunkRow;

        // Return mesh if it exists already
        if (polarMeshes.TryGetValue(key, out float[]? mesh))
        {
            return mesh;
        }

        // Make new mesh
        int n = world.chunkSize + 1;
        int startRadius = band.GetRowRadius(chunkRow);

        mesh = new float[n * n * 2];

        for (int j = 0; j < n; j++)
        {
            double radius = startRadius + j;

            for (int i = 0; i < n; i++)
            {
                int index = (j * n + i) * 2;

                // Subtract startRadius in double before it becomes float so numbers stay small
                mesh[index] = (float)(band.sinTable[i] * radius);
                mesh[index + 1] = (float)(band.cosTable[i] * radius - startRadius);
            }
        }

        polarMeshes[key] = mesh;
        return mesh;
    }

    // Mesh for any chunk
    public float[] GetMesh(Chunk chunk)
    {
        if (chunk.band == -1)
        {
            return cartesianMesh;
        }

        return GetPolarMesh(world.bands[chunk.band], chunk.chunkRow);
    }
}