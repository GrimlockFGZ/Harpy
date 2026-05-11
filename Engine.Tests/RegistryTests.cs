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

public struct Health
{
    public int Value;
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

    [Test]
    public void View_WithTwoComponents_ReturnsOnlyEntitiesWithBoth()
    {
        var e1 = _registry.CreateEntity();
        var e2 = _registry.CreateEntity();
        var e3 = _registry.CreateEntity();

        _registry.AddComponent(e1, new Position());
        _registry.AddComponent(e1, new Velocity());

        _registry.AddComponent(e2, new Position());

        _registry.AddComponent(e3, new Velocity());

        var view = _registry.View<Position, Velocity>().ToArray();

        Assert.That(view.Length, Is.EqualTo(1));
        Assert.That(view[0], Is.EqualTo(e1));
    }

    [Test]
    public void View_WithThreeComponents_ReturnsOnlyEntitiesWithAllThree()
    {
        var e1 = _registry.CreateEntity();
        var e2 = _registry.CreateEntity();
        var e3 = _registry.CreateEntity();
        var e4 = _registry.CreateEntity();

        _registry.AddComponent(e1, new Position());
        _registry.AddComponent(e1, new Velocity());
        _registry.AddComponent(e1, new Health());

        _registry.AddComponent(e2, new Position());
        _registry.AddComponent(e2, new Velocity());

        _registry.AddComponent(e3, new Position());
        _registry.AddComponent(e3, new Health());

        _registry.AddComponent(e4, new Velocity());
        _registry.AddComponent(e4, new Health());

        var view = _registry.View<Position, Velocity, Health>().ToArray();

        Assert.That(view.Length, Is.EqualTo(1));
        Assert.That(view[0], Is.EqualTo(e1));
    }

    [Test]
    public void View_DoesNotReturnDestroyedEntities()
    {
        var entity = _registry.CreateEntity();

        _registry.AddComponent(entity, new Position());
        _registry.AddComponent(entity, new Velocity());

        _registry.DestroyEntity(entity);

        var view = _registry.View<Position, Velocity>().ToArray();

        Assert.That(view, Is.Empty);
    }
}
