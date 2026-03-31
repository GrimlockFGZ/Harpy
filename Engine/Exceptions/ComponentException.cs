namespace Engine.Exceptions;


public class MissingComponentException(int entityId, string componentName) : HarpyException($"Entity {entityId} does not have component '{componentName}'.")
{
    public MissingComponentException(int entityId, Type componentType) 
        : this(entityId, componentType.Name) { }
}    
