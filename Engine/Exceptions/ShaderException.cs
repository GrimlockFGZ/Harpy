using HarpyEngine.Exceptions;

namespace Engine.Exceptions;

public class ShaderException : RenderingException
{
    public string ShaderName { get; }
    public string CompileLog { get; }

    public ShaderException(string name, string log) 
        : base($"Shader '{name}' failed to compile!\nLog: {log}")
    {
        ShaderName = name;
        CompileLog = log;
    }
}