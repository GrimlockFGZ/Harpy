using System.Runtime.CompilerServices;

namespace Engine.Physics;

public readonly struct BoxShape(Vector3 halfExtents)
{
    public readonly Vector3 HalfExtents = halfExtents;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AABB ComputeAABB(in Vector3 position, in Quaternion orientation)
    {
        // Create rotation matrix
        Matrix4x4 rot = Matrix4x4.CreateFromQuaternion(orientation);

        // Extract column basis vectors and take absolute values
        Vector3 absX = Vector3.Abs(new Vector3(rot.M11, rot.M21, rot.M31));
        Vector3 absY = Vector3.Abs(new Vector3(rot.M12, rot.M22, rot.M32));
        Vector3 absZ = Vector3.Abs(new Vector3(rot.M13, rot.M23, rot.M33));

        // Project half-extents onto world axes via dot products
        Vector3 worldHalfExtents = new(
            Vector3.Dot(absX, HalfExtents),
            Vector3.Dot(absY, HalfExtents),
            Vector3.Dot(absZ, HalfExtents)
        );

        return new AABB(position - worldHalfExtents, position + worldHalfExtents);
    }
}