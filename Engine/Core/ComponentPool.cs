using System.Runtime.CompilerServices;
using Engine.Exceptions;

namespace Engine.Core;

/// <summary>
/// Sparse-set storage for a component type T.
/// Dense arrays store packed entities + component data for cache-friendly iteration.
/// Sparse array maps entityId -> denseIndex, or -1 when missing.
/// </summary>
internal sealed class ComponentPool<T> : IComponentPool where T : struct
{
    private int[] _sparse = [];
    private Entity[] _denseEntities = [];
    private T[] _denseData = [];
    private int _count;

    public int Count => _count;

    public ReadOnlySpan<Entity> DenseEntities => _denseEntities.AsSpan(0, _count);
    public Span<T> DenseData => _denseData.AsSpan(0, _count);

    public void EnsureEntityCapacity(int entityId)
    {
        if (entityId < _sparse.Length) return;

        var oldLen = _sparse.Length;
        var newLen = oldLen == 0 ? 64 : oldLen;
        while (newLen <= entityId) newLen *= 2;

        Array.Resize(ref _sparse, newLen);

        for (var i = oldLen; i < newLen; i++)
            _sparse[i] = -1;
    }

    private void EnsureDenseCapacity(int desiredCount)
    {
        if (desiredCount <= _denseData.Length) return;

        var newLen = _denseData.Length == 0 ? 64 : _denseData.Length * 2;
        while (newLen < desiredCount) newLen *= 2;

        Array.Resize(ref _denseData, newLen);
        Array.Resize(ref _denseEntities, newLen);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Has(Entity entity)
    {
        var id = entity.Id;
        return (uint)id < (uint)_sparse.Length && _sparse[id] >= 0;
    }

    public ref T AddOrReplace(Entity entity, in T component)
    {
        var id = entity.Id;
        EnsureEntityCapacity(id);

        var denseIndex = _sparse[id];
        if (denseIndex >= 0)
        {
            _denseData[denseIndex] = component;
            return ref _denseData[denseIndex];
        }

        EnsureDenseCapacity(_count + 1);

        denseIndex = _count++;
        _denseEntities[denseIndex] = entity;
        _denseData[denseIndex] = component;
        _sparse[id] = denseIndex;

        return ref _denseData[denseIndex];
    }

    public ref T GetRef(Entity entity)
    {
        if (!Has(entity))
            throw new MissingComponentException(entity.Id,typeof(T));

        return ref _denseData[_sparse[entity.Id]];
    }

    public bool Remove(Entity entity)
    {
        if (!Has(entity)) return false;

        var id = entity.Id;
        var removeIndex = _sparse[id];
        var lastIndex = _count - 1;

        if (removeIndex != lastIndex)
        {
            // Move last element into the removed slot
            var movedEntity = _denseEntities[lastIndex];
            _denseEntities[removeIndex] = movedEntity;
            _denseData[removeIndex] = _denseData[lastIndex];
            _sparse[movedEntity.Id] = removeIndex;
        }

        _denseEntities[lastIndex] = default;
        _denseData[lastIndex] = default;
        _sparse[id] = -1;
        _count--;

        return true;
    }

    bool IComponentPool.Has(Entity entity) => Has(entity);
    bool IComponentPool.Remove(Entity entity) => Remove(entity);
}