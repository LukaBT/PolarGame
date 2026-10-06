using System.Numerics;

public class World
{
    public int radius = 50000;
    public int chunkSize = 128;
    public int startingColumnChunks = 14;
    public int coreRadius = 128 * 3; // 384
    public int coreRadiusChunks = 3;

    public String worldType = "Polar";

    public int bandCount;
    public Band[] bands = null!;
    public Core core = null!;

    // Seed for the whole world, flat terrain does not use it yet
    public long seed = 0;

    // Vertices and indices for drawing chunks
    public ChunkMesh mesh = null!;

    public TerrainGenerator generator = null!;

    // Polar world constructor
    public World(String worldType, int radius, int coreRadius, int startingColumnChunks, int chunkSize, int bandCount)
    {
        this.radius = radius;
        this.coreRadius = coreRadius;
        this.coreRadiusChunks = coreRadius / chunkSize;
        this.startingColumnChunks = startingColumnChunks;
        this.chunkSize = chunkSize;
        this.worldType = worldType;
        this.bandCount = bandCount;

        // Radius is sea level, bands keep going above it
        bands = new Band[bandCount];
        for (int i = 0; i < bandCount; i++)
        {
            bands[i] = new Band(this, i);
        }

        core = new Core(this);

        mesh = new ChunkMesh(this);
        generator = new TerrainGenerator(this);
    }
    // Cartesian world constructor
    public World(String worldType, int chunkSize)
    {
        this.worldType = worldType;
        this.chunkSize = chunkSize;

        core = new Core(this);

        mesh = new ChunkMesh(this);
        generator = new TerrainGenerator(this);
    }

    // Finds which block of which band a position is in
    // Returns band index or -1 if position is not inside any band
    public int PositionToBlock(double x, double y, out int blockColumn, out int blockRow)
    {
        blockColumn = 0;
        blockRow = 0;

        double radius = Math.Sqrt(x * x + y * y);

        if (radius < coreRadius || radius >= bands[bandCount - 1].outerRadius)
        {
            return -1;
        }

        // Every band is twice as tall as the one before, so band index is log2 of distance in core radiuses
        int bandIndex = BitOperations.Log2((uint)(radius / coreRadius));
        Band band = bands[bandIndex];

        // Angle goes clockwise from straight up
        double angle = Math.Atan2(x, y);
        if (angle < 0)
        {
            angle += Math.Tau;
        }

        blockColumn = band.WrapBlockColumn((int)(angle / band.delta));
        blockRow = (int)(radius - band.innerRadius);

        return bandIndex;
    }

    private bool IsInCore(double x, double y)
    {
        return worldType == "Cartesian" || x * x + y * y < (double)coreRadius * coreRadius;
    }


    // Get block by position in the world
    public ushort GetBlock(double x, double y)
    {
        if (IsInCore(x, y))
        {
            return core.GetBlock((int)Math.Floor(x), (int)Math.Floor(y));
        }

        int bandIndex = PositionToBlock(x, y, out int blockColumn, out int blockRow);

        if (bandIndex == -1)
        {
            return 0;
        }

        return bands[bandIndex].GetBlock(blockColumn, blockRow);
    }

    // Set block by position in the world, returns false if block could not be set
    public bool SetBlock(double x, double y, ushort blockId)
    {
        if (IsInCore(x, y))
        {
            return core.SetBlock((int)Math.Floor(x), (int)Math.Floor(y), blockId);
        }

        int bandIndex = PositionToBlock(x, y, out int blockColumn, out int blockRow);

        if (bandIndex == -1)
        {
            return false;
        }

        return bands[bandIndex].SetBlock(blockColumn, blockRow, blockId);
    }

    // Get block by band address, rows outside of the band continue into the neighbour
    public ushort GetBlock(int bandIndex, int blockColumn, int blockRow)
    {
        Band band = bands[bandIndex];

        if (blockRow >= 0 && blockRow < band.bandChunkHeight * chunkSize)
        {
            return band.GetBlock(blockColumn, blockRow);
        }

        band.GetBlockCenter(blockColumn, blockRow, out double x, out double y);
        return GetBlock(x, y);
    }

    public bool SetBlock(int bandIndex, int blockColumn, int blockRow, ushort blockId)
    {
        Band band = bands[bandIndex];

        if (blockRow >= 0 && blockRow < band.bandChunkHeight * chunkSize)
        {
            return band.SetBlock(blockColumn, blockRow, blockId);
        }

        band.GetBlockCenter(blockColumn, blockRow, out double x, out double y);
        return SetBlock(x, y, blockId);
    }
}