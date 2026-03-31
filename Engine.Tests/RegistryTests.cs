using Engine.Core;
using Engine.Exceptions;
using NUnit.Framework;

namespace Engine.Tests;

public struct Position
{
    public float X, Y;
}

public struct Velocity
{
    public float X, Y;
}

[TestFixture]
public class RegistryTests
{
    private Registry _registry;

    [SetUp]
    public void Setup()
    {
        _registry = new Registry();
    }

    [Test]
    public void CreateEntity_ReturnsUniqueEntities()
    {
        var e1 = _registry.CreateEntity();
        var e2 = _registry.CreateEntity();

        Assert.That(e1, Is.Not.EqualTo(e2));
    }

    [Test]
    public void AddComponent_StoresCorrectData()
    {
        var entity = _registry.CreateEntity();
        var pos = new Position { X = 10, Y = 20 };

        _registry.AddComponent(entity, pos);

        Assert.That(_registry.HasComponent<Position>(entity), Is.True);
        Assert.That(_registry.GetComponent<Position>(entity).X, Is.EqualTo(10));
        Assert.That(_registry.GetComponent<Position>(entity).Y, Is.EqualTo(20));
    }

    [Test]
    public void RemoveComponent_RemovesComponent()
    {
        var entity = _registry.CreateEntity();
        _registry.AddComponent(entity, new Position { X = 10, Y = 20 });

        var removed = _registry.RemoveComponent<Position>(entity);

        Assert.That(removed, Is.True);
        Assert.That(_registry.HasComponent<Position>(entity), Is.False);
    }

    [Test]
    public void GetComponent_ThrowsIfMissing()
    {
        var entity = _registry.CreateEntity();
        
        Assert.Throws<MissingComponentException>(() => _registry.GetComponent<Position>(entity));
    }

    [Test]
    public void AddComponent_UpdatesExistingComponent()
    {
        var entity = _registry.CreateEntity();
        _registry.AddComponent(entity, new Position { X = 1, Y = 1 });
        _registry.AddComponent(entity, new Position { X = 2, Y = 2 });

        Assert.That(_registry.GetComponent<Position>(entity).X, Is.EqualTo(2));
    }

    [Test]
    public void ViewEntities_ReturnsCorrectEntities()
    {
        var e1 = _registry.CreateEntity();
        var e2 = _registry.CreateEntity();
        var e3 = _registry.CreateEntity();

        _registry.AddComponent(e1, new Position());
        _registry.AddComponent(e3, new Position());

        var view = _registry.ViewEntities<Position>();

        Assert.That(view.Length, Is.EqualTo(2));
        Assert.That(view.Contains(e1), Is.True);
        Assert.That(view.Contains(e3), Is.True);
        Assert.That(view.Contains(e2), Is.False);
    }
}
