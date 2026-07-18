using System.Runtime.CompilerServices;

namespace Engine;

[System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
public readonly struct Vector3(float x, float y, float z) : IEquatable<Vector3>
{
    public readonly float X = x;
    public readonly float Y = y;
    public readonly float Z = z;

    // --- Constructors ---
    public Vector3(float uniformScale) : this(uniformScale, uniformScale, uniformScale) { }

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
    public static Vector3 operator +(Vector3 left, Vector3 right) => 
        new(left.X + right.X, left.Y + right.Y, left.Z + right.Z);

    public static Vector3 operator -(Vector3 left, Vector3 right) => 
        new(left.X - right.X, left.Y - right.Y, left.Z - right.Z);

    public static Vector3 operator *(Vector3 vector, float scalar) => 
        new(vector.X * scalar, vector.Y * scalar, vector.Z * scalar);

    public static Vector3 operator *(float scalar, Vector3 vector) => vector * scalar;

    public static Vector3 operator /(Vector3 vector, float divisor) => 
        new(vector.X / divisor, vector.Y / divisor, vector.Z / divisor);

    public static Vector3 operator -(Vector3 vector) => 
        new(-vector.X, -vector.Y, -vector.Z);

    // --- Equality Logic ---
    private const float Epsilon = 1e-6f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Vector3 left, Vector3 right) => 
        MathF.Abs(left.X - right.X) < Epsilon && 
        MathF.Abs(left.Y - right.Y) < Epsilon && 
        MathF.Abs(left.Z - right.Z) < Epsilon;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Vector3 left, Vector3 right) => !(left == right);

    // --- Products ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Dot(Vector3 left, Vector3 right) => 
        (left.X * right.X) + (left.Y * right.Y) + (left.Z * right.Z);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Cross(Vector3 left, Vector3 right) => new(
        (left.Y * right.Z) - (left.Z * right.Y),
        (left.Z * right.X) - (left.X * right.Z),
        (left.X * right.Y) - (left.Y * right.X)
    );

    // --- Length and Normalization ---
    public float MagnitudeSquared => (X * X) + (Y * Y) + (Z * Z);
    public float Magnitude => MathF.Sqrt(MagnitudeSquared);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Distance(Vector3 start, Vector3 end) => (end - start).Magnitude;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float DistanceSquared(Vector3 start, Vector3 end) => (end - start).MagnitudeSquared;

    public Vector3 Normalized()
    {
        var length = Magnitude;
        return length > Epsilon ? this / length : Zero;
    }

    // --- Linear Interpolation ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 Lerp(Vector3 start, Vector3 end, float interpolationFactor)
    {
        interpolationFactor = Math.Clamp(interpolationFactor, 0.0f, 1.0f);
        return new Vector3(
            start.X + (end.X - start.X) * interpolationFactor,
            start.Y + (end.Y - start.Y) * interpolationFactor,
            start.Z + (end.Z - start.Z) * interpolationFactor
        );
    }

    // --- Boilerplate & Interfaces ---
    public bool Equals(Vector3 other) => this == other;
    public override bool Equals(object? obj) => obj is Vector3 other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y, Z);
    public override string ToString() => $"Vector3(X: {X}, Y: {Y}, Z: {Z})";
}