using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using SNQuaternion = System.Numerics.Quaternion;
using SNVector4 = System.Numerics.Vector4;

namespace Engine;

[StructLayout(LayoutKind.Sequential)]
public readonly struct Quaternion : IEquatable<Quaternion>, IFormattable
{
    // Backed by SIMD for AOT performance
    private readonly SNVector4 _v;

    public float X { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _v.X; }
    public float Y { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _v.Y; }
    public float Z { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _v.Z; }
    public float W { [MethodImpl(MethodImplOptions.AggressiveInlining)] get => _v.W; }

    // --- Static Constants ---
    public static readonly Quaternion Identity = new(0f, 0f, 0f, 1f);
    public static readonly Quaternion Zero = new(0f, 0f, 0f, 0f);

    private static readonly SNVector4 ConjugateMask = new(-1f, -1f, -1f, 1f);

    // --- Constructors ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Quaternion(float x, float y, float z, float w) => _v = new SNVector4(x, y, z, w);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Quaternion(Vector3 xyz, float w) => _v = new SNVector4(xyz.X, xyz.Y, xyz.Z, w);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Quaternion(SNVector4 v) => _v = v;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Quaternion(SNQuaternion q) => _v = new SNVector4(q.X, q.Y, q.Z, q.W);

    // --- Properties ---
    public float LengthSquared => SNVector4.Dot(_v, _v);
    public float Length => _v.Length();
    public Vector3 XYZ => new(X, Y, Z);

    // --- Core Operations ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion Normalize(Quaternion q) => new(SNVector4.Normalize(q._v));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Dot(Quaternion a, Quaternion b) => SNVector4.Dot(a._v, b._v);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion Conjugate(Quaternion q) => new(q._v * ConjugateMask);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion Inverse(Quaternion q) => 
        new(SNQuaternion.Inverse(q));

    // --- Vector Rotation ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 Rotate(Vector3 v)
    {
        return System.Numerics.Vector3.Transform(v, (SNQuaternion)this);
    }

    /// <summary>Rotates a vector by the inverse (conjugate) of this quaternion.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 InverseRotate(Vector3 v)
    {
        return System.Numerics.Vector3.Transform(v, SNQuaternion.Conjugate((SNQuaternion)this));
    }

    // --- Factories & Rotation Utilities ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion FromAxisAngle(Vector3 axis, float angleRadians) =>
        new(SNQuaternion.CreateFromAxisAngle(axis, angleRadians));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion FromYawPitchRoll(float yaw, float pitch, float roll) =>
        new(SNQuaternion.CreateFromYawPitchRoll(yaw, pitch, roll));

    // --- Interpolation ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion Slerp(Quaternion a, Quaternion b, float t) =>
        new(SNQuaternion.Slerp(a, b, t));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion Lerp(Quaternion a, Quaternion b, float t) =>
        new(SNQuaternion.Lerp(a, b, t));

    // --- Multiplication / Operators ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion operator *(Quaternion a, Quaternion b) =>
        new(SNQuaternion.Multiply(a, b));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion operator -(Quaternion q) => new(-q._v);

    // --- System.Numerics Implicit Conversion Tricks ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SNQuaternion(Quaternion q) => new(q.X, q.Y, q.Z, q.W);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Quaternion(SNQuaternion q) => new(q.X, q.Y, q.Z, q.W);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator SNVector4(Quaternion q) => q._v;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Quaternion(SNVector4 v) => new(v);

    // --- Equality & Hashing ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Quaternion other) => _v == other._v;
    public override bool Equals(object? obj) => obj is Quaternion other && Equals(other);

    public override int GetHashCode()
    {
        // Antipodal symmetry hash: ensures q and -q hash to the same bucket
        var hashVec = W < 0 ? -_v : _v;
        return hashVec.GetHashCode();
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Quaternion left, Quaternion right) => left.Equals(right);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Quaternion left, Quaternion right) => !left.Equals(right);

    // --- Boilerplate ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out float x, out float y, out float z, out float w)
    {
        x = X; y = Y; z = Z; w = W;
    }

    public override string ToString() => $"Quaternion({X}, {Y}, {Z}, {W})";
    public string ToString(string? format, IFormatProvider? provider) => ToString();
}