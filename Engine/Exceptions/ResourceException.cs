namespace Engine.Exceptions;

public class ResourceNotFoundException(string name) : HarpyException($"Resource '{name}' was not found in the manifest.");
public class ResourceLoadException(string name, string reason) : HarpyException($"Failed to load '{name}': {reason}");