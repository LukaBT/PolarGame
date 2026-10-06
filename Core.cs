public class Core
{
    private World world;

    // Loaded chunks
    public Dictionary<long, Chunk> chunks = new Dictionary<long, Chunk>();

    public Core(World world)
    {
        this.world = world;
    }

    private long GetKey(int chunkX, int chunkY)
    {
        return ((long)chunkY << 32) | (uint)chunkX;
    }

    private int FloorDiv(int a, int b)
    {
        int result = a / b;

        // Normal division rounds towards zero, we need it to round down
        if (a % b != 0 && a < 0)
        {
            result--;
        }

        return result;
    }

    // Block (0,0) is the block right and above the center of the planet
    // Chunk (0,0) is centered on the center of the planet so everything is shifted by half a chunk
    private void GetChunkAddress(int blockX, int blockY, out int chunkX, out int chunkY, out int localX, out int localY)
    {
        int shiftedX = blockX + world.chunkSize / 2;
        int shiftedY = blockY + world.chunkSize / 2;

        chunkX = FloorDiv(shiftedX, world.chunkSize);
        chunkY = FloorDiv(shiftedY, world.chunkSize);

        localX = ((shiftedX % world.chunkSize) + world.chunkSize) % world.chunkSize;
        localY = ((shiftedY % world.chunkSize) + world.chunkSize) % world.chunkSize;
    }


    // World position of the bottom left corner of a chunk
    public void GetChunkOrigin(int chunkX, int chunkY, out double x, out double y)
    {
        x = chunkX * world.chunkSize - world.chunkSize / 2;
        y = chunkY * world.chunkSize - world.chunkSize / 2;
    }


    // Returns null if chunk is not loaded
    public Chunk? GetChunk(int chunkX, int chunkY)
    {
        chunks.TryGetValue(GetKey(chunkX, chunkY), out Chunk? chunk);
        return chunk;
    }

    public Chunk LoadChunk(int chunkX, int chunkY)
    {
        long key = GetKey(chunkX, chunkY);

        if (!chunks.TryGetValue(key, out Chunk? chunk))
        {
            chunk = new Chunk(world, chunkX, chunkY);
            chunks[key] = chunk;
        }

        return chunk;
    }

    public void UnloadChunk(int chunkX, int chunkY)
    {
        chunks.Remove(GetKey(chunkX, chunkY));
    }


    public ushort GetBlock(int blockX, int blockY)
    {
        GetChunkAddress(blockX, blockY, out int chunkX, out int chunkY, out int localX, out int localY);

        Chunk? chunk = GetChunk(chunkX, chunkY);

        if (chunk == null)
        {
            return 0;
        }

        return chunk.GetBlock(localX, localY);
    }

    // Returns false if block could not be set
    public bool SetBlock(int blockX, int blockY, ushort blockId)
    {
        // On a planet blocks only exist where the center of the block is closer than coreRadius
        if (world.worldType == "Polar")
        {
            double centerX = blockX + 0.5;
            double centerY = blockY + 0.5;
            double coreRadius = world.coreRadius;

            if (centerX * centerX + centerY * centerY >= coreRadius * coreRadius)
            {
                return false;
            }
        }

        GetChunkAddress(blockX, blockY, out int chunkX, out int chunkY, out int localX, out int localY);

        Chunk? chunk = GetChunk(chunkX, chunkY);

        if (chunk == null)
        {
            return false;
        }

        chunk.SetBlock(localX, localY, blockId);
        return true;
    }
}