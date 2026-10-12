using OpenTK.Graphics.OpenGL4;

// Shader that draws one chunk: the fragment shader looks up the block of every pixel
// in the chunk's R16UI texture, air (0) is discarded.
public class ChunkShader : IDisposable
{
    private const string VertexSource = @"
#version 330 core
layout(location = 0) in vec2 aPos;

uniform vec2 uOrigin;   // chunk origin relative to the camera
uniform vec2 uRot;      // (sin, cos) of the chunk angle, clockwise from up
uniform vec2 uScale;    // world units to clip space
uniform vec2 uView;     // (sin, cos) of the camera angle, clockwise from up
uniform int uGridSize;  // vertices per row

out vec2 vGrid;

void main()
{
    // Rotate the chunk mesh clockwise by its angle, then move it next to the camera
    vec2 p = vec2(aPos.x * uRot.y + aPos.y * uRot.x,
                 -aPos.x * uRot.x + aPos.y * uRot.y) + uOrigin;

    // Turn the whole view by the camera angle, so the camera's up is the screen's up
    p = vec2(p.x * uView.y - p.y * uView.x,
             p.x * uView.x + p.y * uView.y);

    gl_Position = vec4(p * uScale, 0.0, 1.0);

    // Position of this vertex in the block grid, interpolated to find the block of each pixel
    vGrid = vec2(gl_VertexID % uGridSize, gl_VertexID / uGridSize);
}";

    // PALETTE_SIZE is replaced with BlockPalette.Size
    private const string FragmentSource = @"
#version 330 core
in vec2 vGrid;

uniform usampler2D uBlocks;
uniform int uChunkSize;
uniform vec4 uColors[PALETTE_SIZE];

out vec4 outColor;

void main()
{
    ivec2 b = clamp(ivec2(floor(vGrid)), ivec2(0), ivec2(uChunkSize - 1));
    uint id = texelFetch(uBlocks, b, 0).r;

    if (id == 0u) discard;

    outColor = uColors[min(id, uint(PALETTE_SIZE - 1))];
}";

    private readonly ShaderProgram program;
    private readonly int uOrigin, uRot, uScale, uView;

    public ChunkShader(int chunkSize, BlockPalette palette)
    {
        string fragment = FragmentSource.Replace("PALETTE_SIZE", BlockPalette.Size.ToString());
        program = new ShaderProgram(VertexSource, fragment, "Chunk");

        uOrigin = program.GetUniform("uOrigin");
        uRot = program.GetUniform("uRot");
        uScale = program.GetUniform("uScale");
        uView = program.GetUniform("uView");

        // These never change
        program.Use();
        GL.Uniform1(program.GetUniform("uGridSize"), chunkSize + 1);
        GL.Uniform1(program.GetUniform("uChunkSize"), chunkSize);
        GL.Uniform1(program.GetUniform("uBlocks"), 0);
        GL.Uniform4(program.GetUniform("uColors"), BlockPalette.Size, palette.colors);
    }

    public void Use()
    {
        program.Use();
    }

    // Once per frame
    public void SetCamera(Camera camera)
    {
        GL.Uniform2(uScale, (float)(2.0 / camera.viewWidth), (float)(2.0 / camera.viewHeight));
        GL.Uniform2(uView, (float)camera.sin, (float)camera.cos);
    }

    // Once per chunk, sin and cos are of the chunk angle (0 and 1 for cartesian chunks)
    public void SetChunk(Camera camera, double originX, double originY, double sin, double cos)
    {
        // Subtract camera in double before it becomes float, so numbers stay small
        GL.Uniform2(uOrigin, (float)(originX - camera.x), (float)(originY - camera.y));
        GL.Uniform2(uRot, (float)sin, (float)cos);
    }

    public void Dispose()
    {
        program.Dispose();
    }
}