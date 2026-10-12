using OpenTK.Graphics.OpenGL4;

public class Renderer : IDisposable
{
    public ChunkRenderer chunks;
    public Overlay overlay;
    public OverlayFont font;
    
    public Renderer(World world)
    {
        GL.ClearColor(0.05f, 0.07f, 0.15f, 1f); // Backgrond color

        chunks = new ChunkRenderer(world); 
        overlay = new Overlay();
        font = new OverlayFont(overlay);
    }

    public void Render(Camera camera, DebugOverlay? debug)
    {
        GL.Clear(ClearBufferMask.ColorBufferBit); // Clear the screen to the background color

        chunks.Render(camera);
        // EntityRenderer goes here, after chunks and before the overlay

        overlay.Begin(camera);
        debug?.Draw(); // Draws chunk outlines, block outline and HUD text
        overlay.Flush(); // Draws everything in one call
    }

    public void Dispose()
    {
        overlay.Dispose();
        chunks.Dispose();
    }
}