using OpenTK.Graphics.OpenGL4;
using StbImageSharp;

// Cuts block_atlas.png into single textures and uploads them as one GL_TEXTURE_2D_ARRAY.
// Layer number = block id. Layer 0 (air is never drawn) and every id without a texture
// get a magenta/black checkerboard, so a missing texture is easy to see.
//
// A texture array is used instead of sampling the atlas directly because every layer has
// its own mipmaps and its own edges: zoomed out, neighbouring textures can not bleed into each other.
public class BlockTextureArray : IDisposable
{
    private readonly int handle;

    public readonly int layerCount;
    public readonly int tileSize;

    public BlockTextureArray(BlockRegistry blocks)
    {
        AtlasSheet sheet = blocks.sheet;

        if (sheet.Entries.Count == 0)
        {
            throw new InvalidDataException("The block atlas has no textures");
        }

        tileSize = sheet.Entries[0].Width;
        layerCount = blocks.Size;

        ImageResult image = LoadImage(AssetPaths.Get(sheet.FileName), sheet);

        int layerBytes = tileSize * tileSize * 4;
        byte[] pixels = new byte[layerBytes * layerCount];

        for (int layer = 0; layer < layerCount; layer++)
        {
            FillMissing(pixels, layer * layerBytes);
        }

        foreach (AtlasEntry entry in sheet.Entries)
        {
            if (entry.Width != tileSize || entry.Height != tileSize)
            {
                throw new InvalidDataException("Texture '" + entry.Name + "' is " + entry.Width + "x" + entry.Height +
                    ", every block texture must be " + tileSize + "x" + tileSize);
            }

            CopyTile(image, entry, pixels, entry.Id * layerBytes);
        }

        handle = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2DArray, handle);

        GL.TexImage3D(TextureTarget.Texture2DArray, 0, PixelInternalFormat.Rgba8,
            tileSize, tileSize, layerCount, 0, PixelFormat.Rgba, PixelType.UnsignedByte, pixels);
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2DArray);

        // Close up: crisp pixels. Far away: the nearest two mip levels are blended so it does not shimmer.
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.NearestMipmapLinear);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
        GL.TexParameter(TextureTarget.Texture2DArray, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);

        GL.BindTexture(TextureTarget.Texture2DArray, 0);
    }

    // Binds to the texture unit that is currently active
    public void Bind()
    {
        GL.BindTexture(TextureTarget.Texture2DArray, handle);
    }

    private static ImageResult LoadImage(string path, AtlasSheet sheet)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException("Block atlas image not found. Is the Assets folder copied to the output folder?", path);
        }

        ImageResult image;
        using (Stream stream = File.OpenRead(path))
        {
            // Rows are top to bottom, the same as the y of the atlas index
            image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha);
        }

        if (image.Width != sheet.Width || image.Height != sheet.Height)
        {
            throw new InvalidDataException(sheet.FileName + " is " + image.Width + "x" + image.Height +
                " but atlas_index.txt says " + sheet.Width + "x" + sheet.Height);
        }

        return image;
    }

    private void CopyTile(ImageResult image, AtlasEntry entry, byte[] destination, int destinationOffset)
    {
        int rowBytes = tileSize * 4;

        for (int row = 0; row < tileSize; row++)
        {
            int source = ((entry.Y + row) * image.Width + entry.X) * 4;
            System.Buffer.BlockCopy(image.Data, source, destination, destinationOffset + row * rowBytes, rowBytes);
        }
    }

    private void FillMissing(byte[] destination, int offset)
    {
        int half = Math.Max(tileSize / 2, 1);

        for (int y = 0; y < tileSize; y++)
        {
            for (int x = 0; x < tileSize; x++)
            {
                bool magenta = ((x / half) + (y / half)) % 2 == 0;
                int i = offset + (y * tileSize + x) * 4;

                destination[i] = (byte)(magenta ? 255 : 0);
                destination[i + 1] = 0;
                destination[i + 2] = (byte)(magenta ? 255 : 0);
                destination[i + 3] = 255;
            }
        }
    }

    public void Dispose()
    {
        GL.DeleteTexture(handle);
    }
}