
namespace Engine.Core.ECS;

public interface ISystem
{
    void Initialize(Scene scene, SceneContext context) { }
    void Update(Scene scene, SceneContext context, float deltaTime) { }
    void Render(Scene scene, SceneContext context, float deltaTime) { }
    void Shutdown(Scene scene, SceneContext context) { }
}