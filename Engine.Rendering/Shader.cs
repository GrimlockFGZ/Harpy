using Engine.Core;
using Engine.Exceptions;
using Silk.NET.OpenGL;

namespace HarpyEngine.Rendering;

/// <summary>
/// Encapsulates an OpenGL shader program, including vertex and fragment shaders.
/// Supports reloading from disk.
/// </summary>
public class Shader
{
    /// <summary>
    /// The handle to the compiled OpenGL shader program.
    /// </summary>
    public uint Handle { get; private set; }

    private readonly GL _gl;
    private readonly string _vPath;
    private readonly string _fPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="Shader"/> class.
    /// </summary>
    /// <param name="gl">The OpenGL context.</param>
    /// <param name="vertexPath">The file path to the vertex shader source.</param>
    /// <param name="fragmentPath">The file path to the fragment shader source.</param>
    public Shader(GL gl, string vertexPath, string fragmentPath)
    {
        _gl = gl;
        _vPath = vertexPath;
        _fPath = fragmentPath;
        Reload();
    }

    /// <summary>
    /// Sets this shader program as the active program in the OpenGL context.
    /// </summary>
    public void Use() => _gl.UseProgram(Handle);

    /// <summary>
    /// Reloads the shader program by recompiling and relinking the source files from disk.
    /// </summary>
    public void Reload()
    {
        try
        {
            var vertex = CompileShader(ShaderType.VertexShader, File.ReadAllText(_vPath));
            var fragment = CompileShader(ShaderType.FragmentShader, File.ReadAllText(_fPath));

            var newHandle = _gl.CreateProgram();
            _gl.AttachShader(newHandle, vertex);
            _gl.AttachShader(newHandle, fragment);
            _gl.LinkProgram(newHandle);

            _gl.GetProgram(newHandle, ProgramPropertyARB.LinkStatus, out var status);
            if (status == 0)
            {
                Logger.LogError($"[Shader Error] Link Fail: {_gl.GetProgramInfoLog(newHandle)}");
                _gl.DeleteProgram(newHandle);
                return;
            }

            // Successfully linked, now swap
            if (Handle != 0) _gl.DeleteProgram(Handle);
            Handle = newHandle;

            _gl.DeleteShader(vertex);
            _gl.DeleteShader(fragment);
            Logger.LogSuccess($"Reloaded: {Path.GetFileName(_vPath)}");
        }
        catch (ResourceNotFoundException ex)
        {
            Logger.LogFatal($"[Shader Error] {ex.Message}");
        }
    }

    /// <summary>
    /// Compiles a single shader stage.
    /// </summary>
    /// <param name="type">The type of shader (e.g., Vertex, Fragment).</param>
    /// <param name="source">The shader source code.</param>
    /// <returns>The handle to the compiled shader.</returns>
    /// <exception cref="Exception">Thrown if shader compilation fails.</exception>
    private uint CompileShader(ShaderType type, string source)
    {
        var shader = _gl.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);

        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out var status);

        // Happy Path: Status 1 (GL_TRUE)
        if (status != 0) return shader;

        // Error Path:
        var infoLog = _gl.GetShaderInfoLog(shader);
        _gl.DeleteShader(shader);

        string typeName = Enum.GetName(type) ?? "UnknownShader";

        throw new ShaderException(typeName, infoLog);
    }
}