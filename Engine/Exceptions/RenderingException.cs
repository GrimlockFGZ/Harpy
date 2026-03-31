using Engine.Exceptions;

namespace HarpyEngine.Exceptions;

public class RenderingException : HarpyException
{
    public RenderingException(string message) : base(message) { }
    
    public RenderingException(string message, Exception innerException) 
        : base(message, innerException) { }
}