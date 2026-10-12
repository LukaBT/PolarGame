using OpenTK.Graphics.OpenGL4;

// GPU side of ChunkMesh: one shared index buffer and one VAO/VBO for every distinct mesh
public class ChunkMeshBuffers : IDisposable
{
    private readonly World world;
    private readonly int ebo;
    private readonly int indexCount;

    // One VAO for every distinct float[] from ChunkMesh (compared by reference)
    private readonly Dictionary<float[], int> vaos =
        new Dictionary<float[], int>(ReferenceEqualityComparer.Instance);
    private readonly List<int> vbos = new List<int>();

    public ChunkMeshBuffers(World world)
    {
        this.world = world;

        int gridSize = world.chunkSize + 1;
        if (gridSize * gridSize > ushort.MaxValue + 1)
        {
            throw new InvalidOperationException("chunkSize too big for ushort indices");
        }

        // Shared index buffer, same for every chunk
        indexCount = world.mesh.indices.Length;
        ebo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);
        GL.BufferData(BufferTarget.ElementArrayBuffer, indexCount * sizeof(ushort),
            world.mesh.indices, BufferUsageHint.StaticDraw);
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, 0);
    }

    public void Draw(float[] mesh)
    {
        GL.BindVertexArray(GetVao(mesh));
        GL.DrawElements(PrimitiveType.Triangles, indexCount, DrawElementsType.UnsignedShort, 0);
    }

    private int GetVao(float[] mesh)
    {
        if (vaos.TryGetValue(mesh, out int vao))
        {
            return vao;
        }

        int vbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, mesh.Length * sizeof(float), mesh, BufferUsageHint.StaticDraw);

        vao = GL.GenVertexArray();
        GL.BindVertexArray(vao);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 2 * sizeof(float), 0);

        // The shared index buffer is remembered by the VAO
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo);

        GL.BindVertexArray(0);
        GL.BindBuffer(BufferTarget.ArrayBuffer, 0);

        vbos.Add(vbo);
        vaos[mesh] = vao;
        return vao;
    }

    public void Dispose()
    {
        foreach (int vao in vaos.Values) GL.DeleteVertexArray(vao);
        vaos.Clear();

        foreach (int vbo in vbos) GL.DeleteBuffer(vbo);
        vbos.Clear();

        GL.DeleteBuffer(ebo);
    }
}