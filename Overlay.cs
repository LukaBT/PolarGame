using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

// Draws lines and rectangles on top of the world.
// Everything is collected as triangles during the frame and drawn in one call by Flush.
// World positions are converted to pixels in double precision, so it works far from the origin.
public class Overlay : IDisposable
{
    private const string VertexSource = @"
#version 330 core
layout(location = 0) in vec2 aPos;
layout(location = 1) in vec4 aColor;
out vec4 vColor;
void main()
{
    vColor = aColor;
    gl_Position = vec4(aPos, 0.0, 1.0);
}";

    private const string FragmentSource = @"
#version 330 core
in vec4 vColor;
out vec4 outColor;
void main()
{
    outColor = vColor;
}";

    private readonly ShaderProgram program;
    private readonly int vao;
    private readonly int vbo;

    // 6 floats per vertex: x, y, r, g, b, a
    private float[] vertices = new float[6 * 6 * 2048];
    private int floatCount = 0;

    private Camera camera = null!;
    private int width = 1, height = 1; // framebuffer size in pixels

    public Overlay()
    {
        program = new ShaderProgram(VertexSource, FragmentSource, "Overlay");

        vao = GL.GenVertexArray();
        vbo = GL.GenBuffer();

        GL.BindVertexArray(vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);

        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 6 * sizeof(float), 0);
        GL.EnableVertexAttribArray(1);
        GL.VertexAttribPointer(1, 4, VertexAttribPointerType.Float, false, 6 * sizeof(float), 2 * sizeof(float));

        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
    }

    // Call once per frame before adding anything
    public void Begin(Camera camera)
    {
        this.camera = camera;
        width = camera.screenWidth;
        height = camera.screenHeight;
        floatCount = 0;
    }

    private double ToPixelX(double worldX, double worldY)
    {
        camera.WorldToView(worldX, worldY, out double viewX, out _);
        return viewX / camera.viewWidth * width + width / 2.0;
    }

    private double ToPixelY(double worldX, double worldY)
    {
        camera.WorldToView(worldX, worldY, out _, out double viewY);
        return height / 2.0 - viewY / camera.viewHeight * height;
    }

    // Thickness is in pixels
    public void WorldLine(double x0, double y0, double x1, double y1, double thickness, Color4 color)
    {
        PixelLine(ToPixelX(x0, y0), ToPixelY(x0, y0), ToPixelX(x1, y1), ToPixelY(x1, y1), thickness, color);
    }

    public void PixelLine(double x0, double y0, double x1, double y1, double thickness, Color4 color)
    {
        double dx = x1 - x0;
        double dy = y1 - y0;
        double length = Math.Sqrt(dx * dx + dy * dy);
        if (length < 1e-9) return;

        // Offset sideways by half of the thickness
        double nx = -dy / length * thickness / 2;
        double ny = dx / length * thickness / 2;

        Quad(x0 + nx, y0 + ny, x0 - nx, y0 - ny, x1 - nx, y1 - ny, x1 + nx, y1 + ny, color);
    }

    // Rectangle in pixels, y goes down from the top of the screen
    public void Rect(double x, double y, double w, double h, Color4 color)
    {
        Quad(x, y, x + w, y, x + w, y + h, x, y + h, color);
    }

    private void Quad(double x0, double y0, double x1, double y1, double x2, double y2, double x3, double y3, Color4 c)
    {
        Vertex(x0, y0, c); Vertex(x1, y1, c); Vertex(x2, y2, c);
        Vertex(x0, y0, c); Vertex(x2, y2, c); Vertex(x3, y3, c);
    }

    private void Vertex(double px, double py, Color4 c)
    {
        if (floatCount + 6 > vertices.Length)
        {
            Array.Resize(ref vertices, vertices.Length * 2);
        }

        vertices[floatCount++] = (float)(px / width * 2 - 1);
        vertices[floatCount++] = (float)(1 - py / height * 2);
        vertices[floatCount++] = c.R;
        vertices[floatCount++] = c.G;
        vertices[floatCount++] = c.B;
        vertices[floatCount++] = c.A;
    }

    // Draws everything that was added since Begin
    public void Flush()
    {
        if (floatCount == 0) return;

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        program.Use();
        GL.BindVertexArray(vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, floatCount * sizeof(float), vertices, BufferUsageHint.StreamDraw);
        GL.DrawArrays(PrimitiveType.Triangles, 0, floatCount / 6);

        GL.BindVertexArray(0);
        GL.Disable(EnableCap.Blend);
        floatCount = 0;
    }

    public void Dispose()
    {
        GL.DeleteBuffer(vbo);
        GL.DeleteVertexArray(vao);
        program.Dispose();
    }
}