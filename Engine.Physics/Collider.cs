using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Engine.Physics;

public enum ShapeType : byte
{
    Sphere,
    Box
}

[StructLayout(LayoutKind.Explicit)]
public readonly struct Collider
{
    [FieldOffset(0)] public readonly ShapeType Type;
    [FieldOffset(4)] public readonly SphereShape Sphere;
    [FieldOffset(4)] public readonly BoxShape Box;

    public Collider(SphereShape sphere)
    {
        Type = ShapeType.Sphere;
        Sphere = sphere;
    }

    public Collider(BoxShape box)
    {
        Type = ShapeType.Box;
        Box = box;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AABB ComputeAABB(in Vector3 position, in Quaternion orientation)
    {
        return Type switch
        {
            ShapeType.Sphere => Sphere.ComputeAABB(position),
            ShapeType.Box => Box.ComputeAABB(position, orientation),
            _ => default
        };
    }
}