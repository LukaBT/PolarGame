// Temporary colors until there is a block registry and a texture atlas, index is block ID
public class BlockPalette
{
    public const int Size = 16;

    // 4 floats (r, g, b, a) per block ID
    public float[] colors = new float[Size * 4];

    public BlockPalette()
    {
        for (int i = 0; i < Size; i++) Set(i, 1f, 0f, 1f); // magenta = unknown block

        Set(1, 0.30f, 0.65f, 0.25f); // grass
        Set(2, 0.45f, 0.45f, 0.48f); // stone
        Set(3, 0.55f, 0.38f, 0.22f); // dirt
    }

    private void Set(int id, float r, float g, float b)
    {
        colors[id * 4] = r;
        colors[id * 4 + 1] = g;
        colors[id * 4 + 2] = b;
        colors[id * 4 + 3] = 1f;
    }
}