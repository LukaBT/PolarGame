using OpenTK.Graphics.OpenGL4;

// Draws every loaded chunk that can be on screen
public class ChunkRenderer : IDisposable
{
    private readonly World world;
    private readonly int chunkSize;

    private readonly BlockTextureArray blockTextures;
    private readonly ChunkShader shader;
    private readonly ChunkMeshBuffers meshBuffers;
    private readonly ChunkTextureCache textures;

    // The block textures are shared by every world, so they are owned (and disposed) by the Game
    public ChunkRenderer(World world, BlockTextureArray blockTextures)
    {
        this.world = world;
        chunkSize = world.chunkSize;

        this.blockTextures = blockTextures;
        shader = new ChunkShader(chunkSize, blockTextures.layerCount);
        meshBuffers = new ChunkMeshBuffers(world);
        textures = new ChunkTextureCache(chunkSize);
    }

    public void Render(Camera camera)
    {
        textures.NextFrame();

        shader.Use();
        shader.SetCamera(camera);

        // Unit 1 holds the block textures for the whole frame
        GL.ActiveTexture(TextureUnit.Texture1);
        blockTextures.Bind();

        // Unit 0 is rebound for every chunk (its block ids) by ChunkTextureCache
        GL.ActiveTexture(TextureUnit.Texture0);

        foreach (Chunk chunk in world.core.chunks.Values)
        {
            DrawChunk(camera, chunk);
        }

        if (world.bands != null)
        {
            foreach (Band band in world.bands)
            {
                foreach (Chunk chunk in band.chunks.Values)
                {
                    DrawChunk(camera, chunk);
                }
            }
        }

        textures.FreeUnused();
    }

    private void DrawChunk(Camera camera, Chunk chunk)
    {
        double originX, originY, sin, cos, centerX, centerY, cullRadius;
        double half = chunkSize / 2.0;

        if (chunk.band == -1)
        {
            world.core.GetChunkOrigin(chunk.chunkColumn, chunk.chunkRow, out originX, out originY);
            sin = 0;
            cos = 1;
            centerX = originX + half;
            centerY = originY + half;
            cullRadius = half * 1.5;
        }
        else
        {
            Band band = world.bands[chunk.band];
            band.GetChunkOrigin(chunk.chunkColumn, chunk.chunkRow, out originX, out originY);

            double angle = band.GetChunkAngle(chunk.chunkColumn);
            sin = Math.Sin(angle);
            cos = Math.Cos(angle);

            band.GetBlockCenter(chunk.chunkColumn * chunkSize + chunkSize / 2,
                                chunk.chunkRow * chunkSize + chunkSize / 2,
                                out centerX, out centerY);

            // Chunk gets wider towards its outer edge
            double outerRadius = band.GetRowRadius(chunk.chunkRow) + chunkSize;
            double width = chunkSize * band.delta * outerRadius;
            cullRadius = 0.5 * Math.Sqrt(width * width + (double)chunkSize * chunkSize) + 1;
        }

        // Skip chunks that can not be on screen
        if (!camera.IsVisible(centerX, centerY, cullRadius))
        {
            return;
        }

        textures.Bind(chunk);
        shader.SetChunk(camera, originX, originY, sin, cos);
        meshBuffers.Draw(world.mesh.GetMesh(chunk));
    }

    public void Dispose()
    {
        textures.Dispose();
        meshBuffers.Dispose();
        shader.Dispose();
    }
}