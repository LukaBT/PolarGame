using System.Diagnostics;

public class Chunk
{
    private World world;

    // Chunk position
    public int band = -1; // Used only in polar chunks, if chunk is cartesian then band = -1
    public int chunkColumn;
    public int chunkRow;

    public ushort[] blockData;

    // Polar chunk constructor
    public Chunk(World world, int band, int chunkColumn, int chunkRow)
    {
        this.world = world;
        blockData = new ushort[world.chunkSize * world.chunkSize];

        this.band = band;
        this.chunkColumn = chunkColumn;
        this.chunkRow = chunkRow;
    }

    // Cartesian chunk constructor
    public Chunk(World world, int chunkColumn, int chunkRow)
    {
        this.world = world;
        this.band = -1;
        blockData = new ushort[world.chunkSize * world.chunkSize];

        this.chunkColumn = chunkColumn;
        this.chunkRow = chunkRow;
    }

    public ushort GetBlock(int x, int y)
    {
        Debug.Assert(x >= 0 && x < world.chunkSize && y >= 0 && y < world.chunkSize);

        return blockData[y * world.chunkSize + x];
    }

    public void SetBlock(int x, int y, ushort blockId)
    {
        Debug.Assert(x >= 0 && x < world.chunkSize && y >= 0 && y < world.chunkSize);

        blockData[y * world.chunkSize + x] = blockId;
    }
}