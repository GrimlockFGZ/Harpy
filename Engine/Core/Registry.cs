using HarpyEngine.Exceptions;
using System.Runtime.CompilerServices;

namespace Engine.Core;

public readonly record struct Entity(int Id);

internal interface IComponentPool
{
    int Count { get; }
    bool Has(Entity entity);
    bool Remove(Entity entity);
}

public sealed class Registry
{
    private int _entityCount;
    private readonly HashSet<Entity> _entities = [];
    private readonly Dictionary<Type, IComponentPool> _pools = [];

    public IEnumerable<Entity> GetAllEntities() => _entities;

    public Entity CreateEntity()
    {
        var entity = new Entity(_entityCount++);
        _entities.Add(entity);
        Event<EntityCreated>.Invoke(new EntityCreated(entity));
        return entity;
    }

    public void DestroyEntity(Entity entity)
    {
        if (!_entities.Remove(entity)) return;

        foreach (var pool in _pools.Values)
        {
            pool.Remove(entity);
        }

        Event<EntityDestroyed>.Invoke(new EntityDestroyed(entity));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureAlive(Entity entity)
    {
        if (!_entities.Contains(entity))
            throw new EntityNotAliveException(entity.Id);
    }

    private ComponentPool<T> Pool<T>() where T : struct
    {
        var type = typeof(T);
        if (_pools.TryGetValue(type, out var pool))
            return (ComponentPool<T>)pool;

        var created = new ComponentPool<T>();
        _pools[type] = created;
        return created;
    }

    public ref T AddComponent<T>(Entity entity, in T component) where T : struct
    {
        EnsureAlive(entity);
        return ref Pool<T>().AddOrReplace(entity, component);
    }

    public bool HasComponent<T>(Entity entity) where T : struct
    {
        EnsureAlive(entity);
        return Pool<T>().Has(entity);
    }

    public ref T GetComponent<T>(Entity entity) where T : struct
    {
        EnsureAlive(entity);
        return ref Pool<T>().GetRef(entity);
    }

    public bool RemoveComponent<T>(Entity entity) where T : struct
    {
        EnsureAlive(entity);
        return Pool<T>().Remove(entity);
    }

    public ReadOnlySpan<Entity> ViewEntities<T>() where T : struct
        => Pool<T>().DenseEntities;

    public Span<T> ViewData<T>() where T : struct
        => Pool<T>().DenseData;

    /// <summary>
    /// Executes an action on all alive entities that have both component types.
    /// Completely avoids heap allocations and keeps Spans safely on the stack.
    /// </summary>
    public void ForEach<T1, T2>(Action<Entity> action)
        where T1 : struct
        where T2 : struct
    {
        var pool1 = Pool<T1>();
        var pool2 = Pool<T2>();

        if (pool1.Count < pool2.Count)
        {
            ReadOnlySpan<Entity> entities = pool1.DenseEntities;
            for (int i = 0; i < entities.Length; i++)
            {
                if (pool2.Has(entities[i])) action(entities[i]);
            }
        }
        else
        {
            ReadOnlySpan<Entity> entities = pool2.DenseEntities;
            for (int i = 0; i < entities.Length; i++)
            {
                if (pool1.Has(entities[i])) action(entities[i]);
            }
        }
    }

    /// <summary>
    /// Executes an action on all alive entities that have all three component types.
    /// Optimized to choose the smallest iteration path with no reflection or dynamic overhead.
    /// </summary>
    public void ForEach<T1, T2, T3>(Action<Entity> action)
        where T1 : struct
        where T2 : struct
        where T3 : struct
    {
        var p1 = Pool<T1>();
        var p2 = Pool<T2>();
        var p3 = Pool<T3>();

        // Safely determine the smallest pool without 'dynamic' keyword casting
        int c1 = p1.Count;
        int c2 = p2.Count;
        int c3 = p3.Count;

        if (c1 <= c2 && c1 <= c3)
        {
            ReadOnlySpan<Entity> entities = p1.DenseEntities;
            for (int i = 0; i < entities.Length; i++)
            {
                var e = entities[i];
                if (p2.Has(e) && p3.Has(e)) action(e);
            }
        }
        else if (c2 <= c1 && c2 <= c3)
        {
            ReadOnlySpan<Entity> entities = p2.DenseEntities;
            for (int i = 0; i < entities.Length; i++)
            {
                var e = entities[i];
                if (p1.Has(e) && p3.Has(e)) action(e);
            }
        }
        else
        {
            ReadOnlySpan<Entity> entities = p3.DenseEntities;
            for (int i = 0; i < entities.Length; i++)
            {
                var e = entities[i];
                if (p1.Has(e) && p2.Has(e)) action(e);
            }
        }
    }
}