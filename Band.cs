public class Band
{
    private World world;
    public int band;

    public int bandChunkColumns;
    public int bandChunkRows;

    public int blockColumns;

    public int innerRadius;
    public int outerRadius;

    public double delta;

    public double[] sinTable;
    public double[] cosTable;

    public Dictionary<long, Chunk> chunks = new Dictionary<long, Chunk>(); // Loaded chunks
    
    public Band(World world, int band)
    {
        this.world = world;
        this.band = band;

        this.bandChunkRows = world.coreRadiusChunks << band;
        this.bandChunkColumns = world.startingColumnChunks << band;

        this.blockColumns = bandChunkColumns * world.chunkSize;
        
        this.innerRadius = world.coreRadius << band;
        this.outerRadius = innerRadius * 2;


        // Mesh calculation variables
        this.delta = Math.Tau / blockColumns;
 
        this.sinTable = new double[world.chunkSize + 1];
        this.cosTable = new double[world.chunkSize + 1];

        for (int i = 0; i < world.chunkSize + 1; i++)
        {
            sinTable[i] = Math.Sin(i * delta);
            cosTable[i] = Math.Cos(i * delta);
        }
    }

    private long GetKey(int chunkColumn, int chunkRow)
    {
        return ((long)chunkRow << 32) | (uint)chunkColumn;
    }

    public int WrapChunkColumn(int chunkColumn)
    {
        return ((chunkColumn % bandChunkColumns) + bandChunkColumns) % bandChunkColumns;
    }
 
    public int WrapBlockColumn(int blockColumn)
    {
        return ((blockColumn % blockColumns) + blockColumns) % blockColumns;
    }
 
    public int GetRowRadius(int chunkRow)
    {
        return innerRadius + chunkRow * world.chunkSize;
    }
 
    public double GetChunkAngle(int chunkColumn)
    {
        return chunkColumn * world.chunkSize * delta;
    }

    // World position of the bottom left corner of a chunk
    public void GetChunkOrigin(int chunkColumn, int chunkRow, out double x, out double y)
    {
        double angle = GetChunkAngle(chunkColumn);
        int radius = GetRowRadius(chunkRow);

        x = Math.Sin(angle) * radius;
        y = Math.Cos(angle) * radius;
    }


    // World position of the center of a block, row can be outside of the band
    public void GetBlockCenter(int blockColumn, int blockRow, out double x, out double y)
    {
        double angle = (WrapBlockColumn(blockColumn) + 0.5) * delta;
        double radius = innerRadius + blockRow + 0.5;

        x = Math.Sin(angle) * radius;
        y = Math.Cos(angle) * radius;
    }


    // Returns null if chunk is not loaded
    public Chunk? GetChunk(int chunkColumn, int chunkRow)
    {
        chunkColumn = WrapChunkColumn(chunkColumn);

        chunks.TryGetValue(GetKey(chunkColumn, chunkRow), out Chunk? chunk);
        return chunk;
    }

    // Returns null if row is outside of this band
    public Chunk? LoadChunk(int chunkColumn, int chunkRow)
    {
        if (chunkRow < 0 || chunkRow >= bandChunkRows)
        {
            return null;
        }

        chunkColumn = WrapChunkColumn(chunkColumn);
        long key = GetKey(chunkColumn, chunkRow);

        if (!chunks.TryGetValue(key, out Chunk? chunk))
        {
            chunk = new Chunk(world, band, chunkColumn, chunkRow);
            chunks[key] = chunk;
        }

        return chunk;
    }

    public void UnloadChunk(int chunkColumn, int chunkRow)
    {
        chunkColumn = WrapChunkColumn(chunkColumn);
        chunks.Remove(GetKey(chunkColumn, chunkRow));
    }


    public bool GetChunkAddress(int blockColumn, int blockRow, out int chunkColumn, out int chunkRow, out int localX, out int localY)
    {
        chunkColumn = chunkRow = localX = localY = 0;

        if (blockRow < 0 || blockRow >= bandChunkRows * world.chunkSize) return false;

        chunkColumn = WrapBlockColumn(blockColumn) / world.chunkSize;
        chunkRow = blockRow / world.chunkSize;

        localX = ((blockColumn % world.chunkSize) + world.chunkSize) % world.chunkSize;
        localY = ((blockRow % world.chunkSize) + world.chunkSize) % world.chunkSize;
        return true;
    }

    // Block row is counted from the bottom of this band
    public ushort GetBlock(int blockColumn, int blockRow)
    {
        if (!GetChunkAddress(blockColumn, blockRow, out int chunkColumn, out int chunkRow, out int localX, out int localY))
        {
            return 0;
        }

        Chunk? chunk = GetChunk(chunkColumn, chunkRow);

        if (chunk == null)
        {
            return 0;
        }

        return chunk.GetBlock(localX, localY);
    }

    // Returns false if block could not be set
    public bool SetBlock(int blockColumn, int blockRow, ushort blockId)
    {
        if (!GetChunkAddress(blockColumn, blockRow, out int chunkColumn, out int chunkRow, out int localX, out int localY))
        {
            return false;
        }

        Chunk? chunk = GetChunk(chunkColumn, chunkRow);

        if (chunk == null)
        {
            return false;
        }

        chunk.SetBlock(localX, localY, blockId);
        return true;
    }
}