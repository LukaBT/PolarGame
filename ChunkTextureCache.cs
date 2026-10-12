using OpenTK.Graphics.OpenGL4;

// One R16UI texture per chunk that holds its blockData
public class ChunkTextureCache : IDisposable
{
    private const int UnusedFramesBeforeFree = 300;

    private class ChunkTexture
    {
        public int handle;
        public int lastUsedFrame;
    }

    private readonly int chunkSize;
    private readonly Dictionary<Chunk, ChunkTexture> textures = new Dictionary<Chunk, ChunkTexture>();
    private int frame = 0;

    public ChunkTextureCache(int chunkSize)
    {
        this.chunkSize = chunkSize;

        GL.PixelStore(PixelStoreParameter.UnpackAlignment, 1);
    }

    // Call once at the start of every frame
    public void NextFrame()
    {
        frame++;
    }

    // Binds the texture of this chunk, creates it or uploads it again if the chunk changed
    public void Bind(Chunk chunk)
    {
        bool isNew = !textures.TryGetValue(chunk, out ChunkTexture? tex);

        if (isNew)
        {
            tex = new ChunkTexture { handle = GL.GenTexture() };
            textures[chunk] = tex;

            GL.BindTexture(TextureTarget.Texture2D, tex.handle);

            // No mipmaps, so filters must be set or the texture is incomplete
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R16ui, chunkSize, chunkSize, 0,
                PixelFormat.RedInteger, PixelType.UnsignedShort, chunk.blockData);

            chunk.dirty = false;
        }
        else if (chunk.dirty)
        {
            GL.BindTexture(TextureTarget.Texture2D, tex!.handle);
            GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, chunkSize, chunkSize,
                PixelFormat.RedInteger, PixelType.UnsignedShort, chunk.blockData);

            chunk.dirty = false;
        }
        else
        {
            GL.BindTexture(TextureTarget.Texture2D, tex!.handle);
        }

        tex!.lastUsedFrame = frame;
    }

    // Textures of chunks that were unloaded or are far away are freed after a while
    public void FreeUnused()
    {
        List<Chunk>? remove = null;

        foreach (var pair in textures)
        {
            if (frame - pair.Value.lastUsedFrame > UnusedFramesBeforeFree)
            {
                remove ??= new List<Chunk>();
                remove.Add(pair.Key);
            }
        }

        if (remove == null) return;

        foreach (Chunk chunk in remove)
        {
            GL.DeleteTexture(textures[chunk].handle);
            textures.Remove(chunk);
        }
    }

    public void Dispose()
    {
        foreach (ChunkTexture tex in textures.Values) GL.DeleteTexture(tex.handle);
        textures.Clear();
    }
}