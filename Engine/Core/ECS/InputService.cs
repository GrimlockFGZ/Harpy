
namespace Engine.Core.ECS;

public sealed class InputService
{
    private readonly HashSet<string> _keysDown = new();

    public bool IsKeyDown(string key) => _keysDown.Contains(key);

    public void SetKeyDown(string key) => _keysDown.Add(key);

    public void SetKeyUp(string key) => _keysDown.Remove(key);
}