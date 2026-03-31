namespace Engine.Core.Core.ECS;

public sealed class Scene
{
    private readonly List<ISystem> _systems = [];

    public Registry Registry { get; } = new();

    public void AddSystem(ISystem system) => _systems.Add(system);

    public void Initialize(SceneContext context)
    {
        foreach (var system in _systems)
            system.Initialize(this, context);
    }

    public void Update(SceneContext context, float deltaTime)
    {
        foreach (var system in _systems)
            system.Update(this, context, deltaTime);
    }

    public void Render(SceneContext context, float deltaTime)
    {
        foreach (var system in _systems)
            system.Render(this, context, deltaTime);
    }

    public void Shutdown(SceneContext context)
    {
        foreach (var system in _systems)
            system.Shutdown(this, context);
    }
}