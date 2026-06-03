
using HarpyEngine.Exceptions;

namespace Engine.Core;

/// <summary>
/// Represents a unique identifier for an entity within a registry.
/// </summary>
/// <remarks>
/// This is intentionally lightweight. For extra safety (stale entity detection),
/// you can extend this later with a Generation/Version field.
/// </remarks>
public readonly record struct Entity(int Id);

internal interface IComponentPool
{
    void EnsureEntityCapacity(int entityId);
    bool Has(Entity entity);
    bool Remove(Entity entity);
}

/// <summary>
/// Central registry for managing entities and their associated components.
/// Uses sparse-set pools for performance and scalable iteration.
/// </summary>
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

    /// <summary>
    /// Fast view over all entities that have component T.
    /// The returned span is valid until the pool structure changes (add/remove of T).
    /// </summary>
    public ReadOnlySpan<Entity> ViewEntities<T>() where T : struct
        => Pool<T>().DenseEntities;

    /// <summary>
    /// Fast view over dense component data for T (aligned with ViewEntities&lt;T&gt;()).
    /// The returned span is valid until the pool structure changes (add/remove of T).
    /// </summary>
    public Span<T> ViewData<T>() where T : struct
        => Pool<T>().DenseData;

    /// <summary>
    /// Returns all alive entities that have both component types.
    /// </summary>
    public IEnumerable<Entity> View<T1, T2>()
        where T1 : struct
        where T2 : struct
    {
        foreach (var entity in _entities)
        {
            if (Pool<T1>().Has(entity) && Pool<T2>().Has(entity))
                yield return entity;
        }
    }

    /// <summary>
    /// Returns all alive entities that have all three component types.
    /// </summary>
    public IEnumerable<Entity> View<T1, T2, T3>()
        where T1 : struct
        where T2 : struct
        where T3 : struct
    {
        foreach (var entity in _entities)
        {
            if (Pool<T1>().Has(entity) &&
                Pool<T2>().Has(entity) &&
                Pool<T3>().Has(entity))
            {
                yield return entity;
            }
        }
    }
}