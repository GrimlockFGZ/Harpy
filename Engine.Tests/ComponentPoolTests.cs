using NUnit.Framework;
using Engine.Core;

namespace Engine.Tests;

[TestFixture]
public class ComponentPoolTests
{
    [Test]
    public void AddOrReplace_NewEntity_IncreasesCount_Internal()
    {
        var pool = new ComponentPool<Position>();
        var e1 = new Entity(0);
        
        pool.AddOrReplace(e1, new Position { X = 1 });
        
        Assert.That(pool.Count, Is.EqualTo(1));
    }

    [Test]
    public void AddOrReplace_NewEntity_IncreasesCount()
    {
        var registry = new Registry();
        var e1 = registry.CreateEntity();
        
        registry.AddComponent(e1, new Position { X = 1 });
        
        Assert.That(registry.ViewEntities<Position>().Length, Is.EqualTo(1));
    }

    [Test]
    public void AddOrReplace_ExistingEntity_DoesNotIncreaseCount()
    {
        var registry = new Registry();
        var e1 = registry.CreateEntity();
        
        registry.AddComponent(e1, new Position { X = 1 });
        registry.AddComponent(e1, new Position { X = 2 });
        
        Assert.That(registry.ViewEntities<Position>().Length, Is.EqualTo(1));
        Assert.That(registry.GetComponent<Position>(e1).X, Is.EqualTo(2));
    }

    [Test]
    public void Remove_MiddleEntity_SwapsWithLast()
    {
        var registry = new Registry();
        var e1 = registry.CreateEntity();
        var e2 = registry.CreateEntity();
        var e3 = registry.CreateEntity();

        registry.AddComponent(e1, new Position { X = 1 });
        registry.AddComponent(e2, new Position { X = 2 });
        registry.AddComponent(e3, new Position { X = 3 });

        // Dense: [e1, e2, e3]
        registry.RemoveComponent<Position>(e2);
        // Should be: [e1, e3] (e3 swapped into e2's place)

        var entities = registry.ViewEntities<Position>();
        var data = registry.ViewData<Position>();

        Assert.That(entities.Length, Is.EqualTo(2));
        Assert.That(entities[0], Is.EqualTo(e1));
        Assert.That(entities[1], Is.EqualTo(e3));
        
        Assert.That(data[0].X, Is.EqualTo(1));
        Assert.That(data[1].X, Is.EqualTo(3));
    }

    [Test]
    public void EnsureEntityCapacity_ResizesSparseArray()
    {
        var registry = new Registry();
        // Create an entity with a high ID to trigger resizing
        for (int i = 0; i < 100; i++) registry.CreateEntity();
        
        var eLast = registry.CreateEntity();
        Assert.DoesNotThrow(() => registry.AddComponent(eLast, new Position()));
        Assert.That(registry.HasComponent<Position>(eLast), Is.True);
    }
}
