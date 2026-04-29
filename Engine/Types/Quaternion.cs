using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
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

    private const float SlerpEpsilon = 1e-6f;
    private const float NormEpsilon = 1e-10f;
    private static readonly SNVector4 ConjugateMask = new(-1f, -1f, -1f, 1f);

    // --- Constructors ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Quaternion(float x, float y, float z, float w) => _v = new SNVector4(x, y, z, w);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Quaternion(Vector3 xyz, float w) => _v = new SNVector4(xyz.X, xyz.Y, xyz.Z, w);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Quaternion(SNVector4 v) => _v = v;

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

    // --- Vector Rotation (Using your Vector3 Operators) ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 Rotate(Vector3 v)
    {
        Vector3 qv = this.XYZ;
        // Uses your Vector3.Cross and operators
        Vector3 t = Vector3.Cross(qv, v) * 2f;
        return v + (t * W) + Vector3.Cross(qv, t);
    }
    
    /// <summary>Rotates a vector by the inverse (conjugate) of this quaternion.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 InverseRotate(Vector3 v) 
    {
        // For unit quaternions, the inverse is the conjugate.
        // We create a temporary conjugate and use it to rotate the vector.
        return Conjugate(this).Rotate(v);
    }

    // --- Interpolation ---
    public static Quaternion Slerp(Quaternion a, Quaternion b, float t)
    {
        float dot = Dot(a, b);
        Quaternion targetB = b;

        if (dot < 0f) { targetB = new Quaternion(-b._v); dot = -dot; }

        if (dot > 1f - SlerpEpsilon)
        {
            // Linear blend + normalize for speed on nearly identical orientations
            return new Quaternion(SNVector4.Normalize(a._v + (targetB._v - a._v) * t));
        }

        float angle = MathF.Acos(MathF.Max(-1f, MathF.Min(1f, dot)));
        float invSinAngle = 1f / MathF.Sin(angle);
        float sa = MathF.Sin((1f - t) * angle) * invSinAngle;
        float sb = MathF.Sin(t * angle) * invSinAngle;

        return new Quaternion((a._v * sa) + (targetB._v * sb));
    }

    // --- Equality & Hashing ---
    public bool Equals(Quaternion other) => _v == other._v;
    public override bool Equals(object? obj) => obj is Quaternion other && Equals(other);
    
    public override int GetHashCode()
    {
        // Antipodal symmetry hash: ensures q and -q hash to the same bucket
        var hashVec = W < 0 ? -_v : _v;
        return hashVec.GetHashCode();
    }

    public static bool operator ==(Quaternion left, Quaternion right) => left.Equals(right);
    public static bool operator !=(Quaternion left, Quaternion right) => !left.Equals(right);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Quaternion operator *(Quaternion a, Quaternion b)
    {
        // Standard Hamiltonian product: combines two rotations into one
        return new Quaternion(
            a.W * b.X + a.X * b.W + a.Y * b.Z - a.Z * b.Y,
            a.W * b.Y - a.X * b.Z + a.Y * b.W + a.Z * b.X,
            a.W * b.Z + a.X * b.Y - a.Y * b.X + a.Z * b.W,
            a.W * b.W - a.X * b.X - a.Y * b.Y - a.Z * b.Z
        );
    }

    // --- Boilerplate ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out float x, out float y, out float z, out float w)
    {
        x = X; y = Y; z = Z; w = W;
    }

    public override string ToString() => $"Quaternion({X}, {Y}, {Z}, {W})";
    public string ToString(string? format, IFormatProvider? provider) => ToString();
}