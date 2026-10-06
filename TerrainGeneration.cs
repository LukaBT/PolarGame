public class TerrainGenerator
{
    private World world;

    // Block ids, later these come from the item file
    public ushort airId = 0;
    public ushort stoneId = 1;

    public TerrainGenerator(World world)
    {
        this.world = world;
    }

    // Fills every block of the chunk
    public void GenerateChunk(Chunk chunk)
    {
        if (chunk.band == -1)
        {
            GenerateCartesianChunk(chunk);
        }
        else
        {
            GeneratePolarChunk(chunk);
        }
    }

    // On a flat planet the terrain only depends on the row, so every column of the chunk is the same
    private void GeneratePolarChunk(Chunk chunk)
    {
        Band band = world.bands[chunk.band];
        int startRow = chunk.chunkRow * world.chunkSize;

        for (int y = 0; y < world.chunkSize; y++)
        {
            // Radius of the center of the block
            double radius = band.innerRadius + startRow + y + 0.5;

            ushort blockId = airId;
            if (radius < world.radius)
            {
                blockId = stoneId;
            }

            Array.Fill(chunk.blockData, blockId, y * world.chunkSize, world.chunkSize);
        }
    }

    private void GenerateCartesianChunk(Chunk chunk)
    {
        int startX = chunk.chunkColumn * world.chunkSize - world.chunkSize / 2;
        int startY = chunk.chunkRow * world.chunkSize - world.chunkSize / 2;

        // Cartesian only world is a flat world, everything below y = 0 is stone
        if (world.worldType == "Cartesian")
        {
            for (int y = 0; y < world.chunkSize; y++)
            {
                ushort blockId = airId;
                if (startY + y < 0)
                {
                    blockId = stoneId;
                }

                Array.Fill(chunk.blockData, blockId, y * world.chunkSize, world.chunkSize);
            }

            return;
        }

        // On a planet a core block only exists if its center is closer than coreRadius
        double coreLimit = (double)world.coreRadius * world.coreRadius;
        double stoneLimit = (double)world.radius * world.radius;

        for (int y = 0; y < world.chunkSize; y++)
        {
            double centerY = startY + y + 0.5;

            for (int x = 0; x < world.chunkSize; x++)
            {
                double centerX = startX + x + 0.5;
                double distance = centerX * centerX + centerY * centerY;

                ushort blockId = airId;
                if (distance < coreLimit && distance < stoneLimit)
                {
                    blockId = stoneId;
                }

                chunk.blockData[y * world.chunkSize + x] = blockId;
            }
        }
    }
}