namespace Engine.Exceptions;


public class HarpyException : Exception
{
    public HarpyException(string message) : base(message) { }
    public HarpyException(string message, Exception inner) : base(message, inner) { }
}