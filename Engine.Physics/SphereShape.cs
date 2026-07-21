using System.Runtime.CompilerServices;

namespace Engine.Physics;

public readonly struct SphereShape(float radius)
{
    public readonly float Radius = radius;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AABB ComputeAABB(in Vector3 position)
    {
        Vector3 r = new(Radius);
        return new AABB(position - r, position + r);
    }
}