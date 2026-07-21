using System.Runtime.CompilerServices;

namespace Engine.Physics;

[method: MethodImpl(MethodImplOptions.AggressiveInlining)]
public readonly struct ContactPoint(Vector3 position, Vector3 normal, float penetration)
{
    public readonly Vector3 Position = position;
    public readonly Vector3 Normal = normal;
    public readonly float Penetration = penetration;
}

public static class NarrowPhase 
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SphereVsSphere(
        in SphereShape a, in Vector3 posA,
        in SphereShape b, in Vector3 posB,
        out ContactPoint contact)
    {
        Vector3 delta = posB - posA;
        float distSq = delta.MagnitudeSquared;
        float radiusSum = a.Radius + b.Radius;

        if (distSq > radiusSum * radiusSum || distSq < 1e-6f)
        {
            contact = default;
            return false;
        }

        float dist = MathF.Sqrt(distSq);
        Vector3 normal = delta / dist;
        float penetration = radiusSum - dist;
        Vector3 pointPosition = posA + normal * (a.Radius - penetration * 0.5f);

        contact = new ContactPoint(pointPosition, normal, penetration);
        return true;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool SphereVsBox(
        in SphereShape sphere, in Vector3 spherePos,
        in BoxShape box, in Vector3 boxPos, in Quaternion boxOrient,
        out ContactPoint contact)
    {
        // 1. Transform sphere position into box's local space
        var inverseBoxOrient = Quaternion.Conjugate(boxOrient);
        Vector3 localSpherePos = Vector3.Transform(spherePos - boxPos, inverseBoxOrient);

        // 2. Clamp local position to box half-extents (closest point on/in box)
        Vector3 closestLocal = Vector3.Min(
            Vector3.Max(localSpherePos, -box.HalfExtents), 
            box.HalfExtents
        );

        // 3. Transform closest point back to world space
        Vector3 closestWorld = boxPos + Vector3.Transform(closestLocal, boxOrient);

        // 4. Test distance from sphere center to closest point
        Vector3 delta = spherePos - closestWorld;
        float distSq = delta.MagnitudeSquared;

        if (distSq > sphere.Radius * sphere.Radius || distSq < 1e-6f)
        {
            contact = default;
            return false;
        }

        float dist = MathF.Sqrt(distSq);
        Vector3 normal = delta / dist; // Points from box toward sphere
        float penetration = sphere.Radius - dist;

        contact = new ContactPoint(closestWorld, normal, penetration);
        return true;
    }
    
    
    /// <summary>
    /// Oriented-box vs oriented-box collision test using the Separating Axis Theorem (SAT).
    /// Tests 15 candidate axes: 3 face normals of A, 3 face normals of B, and 9 edge-edge
    /// cross products. Zero-allocation, fully unrolled for branch predictability and zero indirection.
    /// </summary>
    /// <param name="boxA">Shape parameters for Box A.</param>
    /// <param name="posA">World position of Box A.</param>
    /// <param name="orientA">World orientation quaternion of Box A.</param>
    /// <param name="boxB">Shape parameters for Box B.</param>
    /// <param name="posB">World position of Box B.</param>
    /// <param name="orientB">World orientation quaternion of Box B.</param>
    /// <param name="contact">Output contact point detailing penetration depth and world-space normal.</param>
    /// <returns>True if the boxes intersect; otherwise, false.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool BoxVsBox(
        in BoxShape boxA, in Vector3 posA, in Quaternion orientA,
        in BoxShape boxB, in Vector3 posB, in Quaternion orientB,
        out ContactPoint contact)
    {
        contact = default;

        // 1. Compute relative transforms in Box A's local coordinate frame
        Quaternion relativeRot = Quaternion.Conjugate(orientA) * orientB;
        Matrix4x4 R = Matrix4x4.CreateFromQuaternion(relativeRot);
        Vector3 worldOffset = posB - posA;
        Vector3 t = orientA.InverseRotate(worldOffset);

        // 2. Absolute rotation matrix with small epsilon cushion to prevent 
        // parallel edge cross-product division/floating-point anomalies.
        const float epsilon = 1e-6f;
        float absR11 = MathF.Abs(R.M11) + epsilon, absR12 = MathF.Abs(R.M12) + epsilon, absR13 = MathF.Abs(R.M13) + epsilon;
        float absR21 = MathF.Abs(R.M21) + epsilon, absR22 = MathF.Abs(R.M22) + epsilon, absR23 = MathF.Abs(R.M23) + epsilon;
        float absR31 = MathF.Abs(R.M31) + epsilon, absR32 = MathF.Abs(R.M32) + epsilon, absR33 = MathF.Abs(R.M33) + epsilon;

        Vector3 hA = boxA.HalfExtents;
        Vector3 hB = boxB.HalfExtents;

        float bestOverlap = float.MaxValue;
        Vector3 bestAxis = Vector3.Zero;

        // Face axes are derived from orthonormal bases (unit vectors), so their
        // projections are already in true world-distance units.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool TestFaceAxis(Vector3 axis, float ra, float rb, float projDist)
        {
            float overlap = ra + rb - MathF.Abs(projDist);
            if (overlap <= 0f) return false;

            if (overlap < bestOverlap)
            {
                bestOverlap = overlap;
                bestAxis = axis; // Already unit length
            }
            return true;
        }

        // Cross products of edges are NOT unit vectors unless perpendicular.
        // We normalize the overlap distance into true world units before comparing
        // against previous best overlaps to prevent latent scale-skewing bugs.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool TestEdgeAxis(Vector3 axis, float ra, float rb, float projDist)
        {
            float SqrMagnitude = axis.MagnitudeSquared;
            if (SqrMagnitude < epsilon) return true; // Parallel edges -> cross product ~ 0, skip axis

            float invLen = 1f / MathF.Sqrt(SqrMagnitude);
            float overlap = (ra + rb - MathF.Abs(projDist)) * invLen;
            if (overlap <= 0f) return false;

            if (overlap < bestOverlap)
            {
                bestOverlap = overlap;
                bestAxis = axis * invLen; // Store pre-normalized unit vector
            }
            return true;
        }

        // ---- 3 Face Normals of Box A ----
        if (!TestFaceAxis(new Vector3(1f, 0f, 0f), hA.X, hB.X * absR11 + hB.Y * absR12 + hB.Z * absR13, t.X)) return false;
        if (!TestFaceAxis(new Vector3(0f, 1f, 0f), hA.Y, hB.X * absR21 + hB.Y * absR22 + hB.Z * absR23, t.Y)) return false;
        if (!TestFaceAxis(new Vector3(0f, 0f, 1f), hA.Z, hB.X * absR31 + hB.Y * absR32 + hB.Z * absR33, t.Z)) return false;

        // ---- 3 Face Normals of Box B ----
        if (!TestFaceAxis(new Vector3(R.M11, R.M21, R.M31), hA.X * absR11 + hA.Y * absR21 + hA.Z * absR31, hB.X, t.X * R.M11 + t.Y * R.M21 + t.Z * R.M31)) return false;
        if (!TestFaceAxis(new Vector3(R.M12, R.M22, R.M32), hA.X * absR12 + hA.Y * absR22 + hA.Z * absR32, hB.Y, t.X * R.M12 + t.Y * R.M22 + t.Z * R.M32)) return false;
        if (!TestFaceAxis(new Vector3(R.M13, R.M23, R.M33), hA.X * absR13 + hA.Y * absR23 + hA.Z * absR33, hB.Z, t.X * R.M13 + t.Y * R.M23 + t.Z * R.M33)) return false;

        // ---- 9 Edge-Edge Cross Product Axes ----
        // A.X x B.X
        if (!TestEdgeAxis(new Vector3(0f, -R.M31, R.M21), hA.Y * absR31 + hA.Z * absR21, hB.Y * absR13 + hB.Z * absR12, t.Z * R.M21 - t.Y * R.M31)) return false;
        // A.X x B.Y
        if (!TestEdgeAxis(new Vector3(0f, -R.M32, R.M22), hA.Y * absR32 + hA.Z * absR22, hB.X * absR13 + hB.Z * absR11, t.Z * R.M22 - t.Y * R.M32)) return false;
        // A.X x B.Z
        if (!TestEdgeAxis(new Vector3(0f, -R.M33, R.M23), hA.Y * absR33 + hA.Z * absR23, hB.X * absR12 + hB.Y * absR11, t.Z * R.M23 - t.Y * R.M33)) return false;

        // A.Y x B.X
        if (!TestEdgeAxis(new Vector3(R.M31, 0f, -R.M11), hA.X * absR31 + hA.Z * absR11, hB.Y * absR23 + hB.Z * absR22, t.X * R.M31 - t.Z * R.M11)) return false;
        // A.Y x B.Y
        if (!TestEdgeAxis(new Vector3(R.M32, 0f, -R.M12), hA.X * absR32 + hA.Z * absR12, hB.X * absR23 + hB.Z * absR21, t.X * R.M32 - t.Z * R.M12)) return false;
        // A.Y x B.Z
        if (!TestEdgeAxis(new Vector3(R.M33, 0f, -R.M13), hA.X * absR33 + hA.Z * absR13, hB.X * absR22 + hB.Y * absR21, t.X * R.M33 - t.Z * R.M13)) return false;

        // A.Z x B.X
        if (!TestEdgeAxis(new Vector3(-R.M21, R.M11, 0f), hA.X * absR21 + hA.Y * absR11, hB.Y * absR33 + hB.Z * absR32, t.Y * R.M11 - t.X * R.M21)) return false;
        // A.Z x B.Y
        if (!TestEdgeAxis(new Vector3(-R.M22, R.M12, 0f), hA.X * absR22 + hA.Y * absR12, hB.X * absR33 + hB.Z * absR31, t.Y * R.M12 - t.X * R.M22)) return false;
        // A.Z x B.Z
        if (!TestEdgeAxis(new Vector3(-R.M23, R.M13, 0f), hA.X * absR23 + hA.Y * absR13, hB.X * absR32 + hB.Y * absR31, t.Y * R.M13 - t.X * R.M23)) return false;

        // ---- Overlap Confirmed: Compute World-Space Contact Normal & Point ----
        // bestAxis is guaranteed to be a unit vector, so orientA.Rotate(bestAxis) preserves unit length.
        Vector3 normalWorld = orientA.Rotate(bestAxis);

        // Ensure normal points from Box A towards Box B
        if (Vector3.Dot(normalWorld, worldOffset) < 0f)
            normalWorld = -normalWorld;

        Vector3 contactPos = posA + normalWorld * (bestOverlap * 0.5f);
        contact = new ContactPoint(contactPos, normalWorld, bestOverlap);
        return true;
    }
    
}