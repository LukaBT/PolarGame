// Loads and generates chunks around the camera
// Later this gets ColumnChunkRenderDistance, RadialChunkRenderDistance and unloading
public class ChunkLoader
{
    private World world;

    public ChunkLoader(World world)
    {
        this.world = world;
    }

    // Loads all core chunks within radiusChunks of the center chunk (0,0)
    public void LoadCore(int radiusChunks)
    {
        for (int cx = -radiusChunks; cx <= radiusChunks; cx++)
        {
            for (int cy = -radiusChunks; cy <= radiusChunks; cy++)
            {
                Chunk chunk = world.core.LoadChunk(cx, cy);
                world.generator.GenerateChunk(chunk);
            }
        }
    }

    // Loads chunks around a position in its band, never unloads
    public void LoadAround(double x, double y)
    {
        if (world.worldType != "Polar") return;

        int bandIndex = world.PositionToBlock(x, y, out int blockColumn, out int blockRow);
        if (bandIndex == -1) return;

        Band band = world.bands[bandIndex];
        int size = world.chunkSize;
        int cc = blockColumn / size;
        int cr = blockRow / size;

        for (int dc = -4; dc <= 4; dc++)
        {
            for (int dr = -3; dr <= 3; dr++)
            {
                int col = band.WrapChunkColumn(cc + dc);
                int row = cr + dr;

                if (band.GetChunk(col, row) != null) continue;

                Chunk? chunk = band.LoadChunk(col, row); // null if row is outside of band
                if (chunk != null) world.generator.GenerateChunk(chunk);
            }
        }
    }
}