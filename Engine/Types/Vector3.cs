using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Engine;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Vector3 : IEquatable<Vector3>
{
    // The trick: Wrap System.Numerics internally. 
    // This gives you full SIMD hardware acceleration with ZERO API changes to the rest of your engine.
    private readonly System.Numerics.Vector3 _vec3SIMD;

    // --- Constructors ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3(float x, float y, float z) => _vec3SIMD = new System.Numerics.Vector3(x, y, z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3(float uniformScale) => _vec3SIMD = new System.Numerics.Vector3(uniformScale);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Vector3(System.Numerics.Vector3 v) => _vec3SIMD = v;

    // --- Properties ---
    public float X => _vec3SIMD.X;
    public float Y => _vec3SIMD.Y;
    public float Z => _vec3SIMD.Z;

    // --- Static Constants ---
    public static Vector3 Zero => new(0.0f, 0.0f, 0.0f);
    public static Vector3 One => new(1.0f, 1.0f, 1.0f);
    public static Vector3 Up => new(0.0f, 1.0f, 0.0f);
    public static Vector3 Down => new(0.0f, -1.0f, 0.0f);
    public static Vector3 Forward => new(0.0f, 0.0f, 1.0f);
    public static Vector3 Backward => new(0.0f, 0.0f, -1.0f);
    public static Vector3 Right => new(1.0f, 0.0f, 0.0f);
    public static Vector3 Left => new(-1.0f, 0.0f, 0.0f);

    // --- Basic Math Operators ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator +(Vector3 left, Vector3 right) => new(left._vec3SIMD + right._vec3SIMD);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator -(Vector3 left, Vector3 right) => new(left._vec3SIMD - right._vec3SIMD);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator *(Vector3 vector, float scalar) => new(vector._vec3SIMD * scalar);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator *(float scalar, Vector3 vector) => vector * scalar;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator /(Vector3 vector, float divisor) => new(vector._vec3SIMD / divisor);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator -(Vector3 vector) => new(-vector._vec3SIMD);

    // --- Exact Equality for Collections ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Vector3 left, Vector3 right) => left._vec3SIMD == right._vec3SIMD;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Vector3 left, Vector3 right) => left._vec3SIMD != right._vec3SIMD;

    // --- Fuzzy Equality ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool Approximately(Vector3 left, Vector3 right, float epsilon = 1e-5f)
    {
        var diff = left - right;
        return diff.MagnitudeSquared < (epsilon * epsilon);
    }

    
    // --- Element-wise Absolute Value ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Abs(Vector3 value) => 
        System.Numerics.Vector3.Abs(value._vec3SIMD);

    // --- Element-wise Multiplication ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator *(Vector3 left, Vector3 right) => 
        new(left._vec3SIMD * right._vec3SIMD);

    // --- Transform by Quaternion ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Transform(Vector3 position, System.Numerics.Quaternion rotation) => 
        System.Numerics.Vector3.Transform(position._vec3SIMD, rotation);
    
    
    // --- Products ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Dot(Vector3 left, Vector3 right) => System.Numerics.Vector3.Dot(left._vec3SIMD, right._vec3SIMD);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Cross(Vector3 left, Vector3 right) => new(System.Numerics.Vector3.Cross(left._vec3SIMD, right._vec3SIMD));

    // --- Length and Normalization ---
    public float MagnitudeSquared => _vec3SIMD.LengthSquared();
    public float Magnitude => _vec3SIMD.Length();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Distance(Vector3 start, Vector3 end) => System.Numerics.Vector3.Distance(start._vec3SIMD, end._vec3SIMD);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float DistanceSquared(Vector3 start, Vector3 end) => System.Numerics.Vector3.DistanceSquared(start._vec3SIMD, end._vec3SIMD);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 Normalized() => new(System.Numerics.Vector3.Normalize(_vec3SIMD));

    // --- Linear Interpolation ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Lerp(Vector3 start, Vector3 end, float factor)
    {
        factor = Math.Clamp(factor, 0.0f, 1.0f);
        return new Vector3(System.Numerics.Vector3.Lerp(start._vec3SIMD, end._vec3SIMD, factor));
    }

    // --- Implicit conversions for System.Numerics use directly ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator System.Numerics.Vector3(Vector3 v) => v._vec3SIMD;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Vector3(System.Numerics.Vector3 v) => new(v);

    // ---Min / Max ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Min(Vector3 left, Vector3 right) =>
        System.Numerics.Vector3.Min(left._vec3SIMD, right._vec3SIMD);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Max(Vector3 left, Vector3 right) =>
        System.Numerics.Vector3.Max(left._vec3SIMD, right._vec3SIMD);
    
    
    // --- Boilerplate ---
    public bool Equals(Vector3 other) => _vec3SIMD.Equals(other._vec3SIMD);
    public override bool Equals(object? obj) => obj is Vector3 other && Equals(other);
    public override int GetHashCode() => _vec3SIMD.GetHashCode();
    public override string ToString() => $"Vector3(X: {X}, Y: {Y}, Z: {Z})";
}