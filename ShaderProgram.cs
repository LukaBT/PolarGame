using OpenTK.Graphics.OpenGL4;

// Compiles and links a vertex + fragment shader, shared by every renderer
public class ShaderProgram : IDisposable
{
    public int handle;

    public ShaderProgram(string vertexSource, string fragmentSource, string name)
    {
        int vs = Compile(ShaderType.VertexShader, vertexSource, name);
        int fs = Compile(ShaderType.FragmentShader, fragmentSource, name);

        handle = GL.CreateProgram();
        GL.AttachShader(handle, vs);
        GL.AttachShader(handle, fs);
        GL.LinkProgram(handle);

        GL.GetProgram(handle, GetProgramParameterName.LinkStatus, out int ok);
        if (ok == 0)
        {
            throw new Exception(name + " shader link error: " + GL.GetProgramInfoLog(handle));
        }

        GL.DeleteShader(vs);
        GL.DeleteShader(fs);
    }

    private static int Compile(ShaderType type, string source, string name)
    {
        int shader = GL.CreateShader(type);
        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out int ok);
        if (ok == 0)
        {
            throw new Exception(name + " " + type + " compile error: " + GL.GetShaderInfoLog(shader));
        }

        return shader;
    }

    public int GetUniform(string uniformName)
    {
        return GL.GetUniformLocation(handle, uniformName);
    }

    public void Use()
    {
        GL.UseProgram(handle);
    }

    public void Dispose()
    {
        GL.DeleteProgram(handle);
    }
}