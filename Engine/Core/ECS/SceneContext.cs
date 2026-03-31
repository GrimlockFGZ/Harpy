namespace Engine.Core.Core.ECS;

public sealed class SceneContext
{
    public required InputService Input { get; init; }
    public required TimeService Time { get; init; }
}