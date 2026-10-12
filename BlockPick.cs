// What is at a position in the world
public struct BlockPick
{
    public bool valid;
    public bool inCore;
    public int band;            // -1 in core
    public int blockA, blockB;  // band: column, row    core: x, y
    public int chunkA, chunkB;  // band: column, row    core: x, y
    public int localX, localY;  // block inside the chunk
}

// Finds the block, chunk and local block at a world position
public class BlockPicker
{
    private World world;

    public BlockPicker(World world)
    {
        this.world = world;
    }

    public BlockPick PickAt(double x, double y)
    {
        BlockPick p = new BlockPick();
        double coreRadius = world.coreRadius;

        if (world.worldType == "Cartesian" || x * x + y * y < coreRadius * coreRadius)
        {
            int bx = (int)Math.Floor(x);
            int by = (int)Math.Floor(y);
            world.core.GetChunkAddress(bx, by, out int cx, out int cy, out int lx, out int ly);

            p.valid = true;
            p.inCore = true;
            p.band = -1;
            p.blockA = bx; p.blockB = by;
            p.chunkA = cx; p.chunkB = cy;
            p.localX = lx; p.localY = ly;
            return p;
        }

        int bandIndex = world.PositionToBlock(x, y, out int column, out int row);
        if (bandIndex == -1) return p;

        Band band = world.bands[bandIndex];
        if (!band.GetChunkAddress(column, row, out int chunkColumn, out int chunkRow, out int localX, out int localY))
        {
            return p;
        }

        p.valid = true;
        p.inCore = false;
        p.band = bandIndex;
        p.blockA = column; p.blockB = row;
        p.chunkA = chunkColumn; p.chunkB = chunkRow;
        p.localX = localX; p.localY = localY;
        return p;
    }

    // The loaded chunk of a pick, null if there is none
    public Chunk? GetChunk(BlockPick p)
    {
        if (!p.valid) return null;

        return p.inCore
            ? world.core.GetChunk(p.chunkA, p.chunkB)
            : world.bands[p.band].GetChunk(p.chunkA, p.chunkB);
    }
}