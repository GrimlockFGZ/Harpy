using Engine.Exceptions;

namespace HarpyEngine.Exceptions;

public class EntityNotAliveException : HarpyException
{
    public int EntityId { get; }

    public EntityNotAliveException(int entityId) 
        : base($"Entity {entityId} is not alive in this Registry. It may have been destroyed.")
    {
        EntityId = entityId;
    }
}