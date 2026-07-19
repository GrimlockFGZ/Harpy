using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Engine;

/// <summary>
/// A 4x4 matrix stored in column-major order, matching OpenGL/GLSL conventions.
/// M{row}{col} naming; e.g. M31 is row 3, column 1.
/// Multiplication order follows GL convention: v' = M * v, and combining
/// transforms is Combine(parent, child) = parent * child (parent applied last).
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct Matrix4x4 : IEquatable<Matrix4x4>
{
    // Column-major storage: M{row}{col}
    public readonly float M11, M21, M31, M41; // column 0
    public readonly float M12, M22, M32, M42; // column 1
    public readonly float M13, M23, M33, M43; // column 2
    public readonly float M14, M24, M34, M44; // column 3

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Matrix4x4(
        float m11, float m12, float m13, float m14,
        float m21, float m22, float m23, float m24,
        float m31, float m32, float m33, float m34,
        float m41, float m42, float m43, float m44)
    {
        M11 = m11; M12 = m12; M13 = m13; M14 = m14;
        M21 = m21; M22 = m22; M23 = m23; M24 = m24;
        M31 = m31; M32 = m32; M33 = m33; M34 = m34;
        M41 = m41; M42 = m42; M43 = m43; M44 = m44;
    }

    public static readonly Matrix4x4 Identity = new(
        1, 0, 0, 0,
        0, 1, 0, 0,
        0, 0, 1, 0,
        0, 0, 0, 1);

    // --- Factory: TRS ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 FromTRS(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        // Rotation from quaternion (row-vector-friendly layout consistent with our column-major storage)
        var x = rotation.X; var y = rotation.Y; var z = rotation.Z; var w = rotation.W;

        var xx = x * x; var yy = y * y; var zz = z * z;
        var xy = x * y; var xz = x * z; var yz = y * z;
        var wx = w * x; var wy = w * y; var wz = w * z;

        var r00 = 1f - 2f * (yy + zz);
        var r01 = 2f * (xy - wz);
        var r02 = 2f * (xz + wy);

        var r10 = 2f * (xy + wz);
        var r11 = 1f - 2f * (xx + zz);
        var r12 = 2f * (yz - wx);

        var r20 = 2f * (xz - wy);
        var r21 = 2f * (yz + wx);
        var r22 = 1f - 2f * (xx + yy);

        return new Matrix4x4(
            r00 * scale.X, r01 * scale.Y, r02 * scale.Z, position.X,
            r10 * scale.X, r11 * scale.Y, r12 * scale.Z, position.Y,
            r20 * scale.X, r21 * scale.Y, r22 * scale.Z, position.Z,
            0f, 0f, 0f, 1f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 FromTransform(Transform t) => FromTRS(t.Position, t.Rotation, t.Scale);

    // --- Factory: View / Projection ---

    /// <summary>
    /// Right-handed look-at view matrix.
    /// </summary>
    public static Matrix4x4 LookAt(Vector3 eye, Vector3 target, Vector3 up)
    {
        var zAxis = (eye - target).Normalized(); // forward is -zAxis
        var xAxis = Vector3.Cross(up, zAxis).Normalized();
        var yAxis = Vector3.Cross(zAxis, xAxis);

        return new Matrix4x4(
            xAxis.X, xAxis.Y, xAxis.Z, -Vector3.Dot(xAxis, eye),
            yAxis.X, yAxis.Y, yAxis.Z, -Vector3.Dot(yAxis, eye),
            zAxis.X, zAxis.Y, zAxis.Z, -Vector3.Dot(zAxis, eye),
            0f, 0f, 0f, 1f);
    }

    /// <summary>
    /// Right-handed perspective projection matrix, GL clip space (Z in [-1, 1]).
    /// </summary>
    public static Matrix4x4 Perspective(float fovYRadians, float aspectRatio, float nearPlane, float farPlane)
    {
        var f = 1f / MathF.Tan(fovYRadians * 0.5f);
        var rangeInv = 1f / (nearPlane - farPlane);

        return new Matrix4x4(
            f / aspectRatio, 0f, 0f, 0f,
            0f, f, 0f, 0f,
            0f, 0f, (farPlane + nearPlane) * rangeInv, 2f * farPlane * nearPlane * rangeInv,
            0f, 0f, -1f, 0f);
    }

    /// <summary>
    /// Orthographic projection matrix, GL clip space (Z in [-1, 1]).
    /// </summary>
    public static Matrix4x4 Orthographic(float width, float height, float nearPlane, float farPlane)
    {
        var rangeInv = 1f / (nearPlane - farPlane);

        return new Matrix4x4(
            2f / width, 0f, 0f, 0f,
            0f, 2f / height, 0f, 0f,
            0f, 0f, 2f * rangeInv, (farPlane + nearPlane) * rangeInv,
            0f, 0f, 0f, 1f);
    }

    // --- Operators ---

// Fix the multiplication math to evaluate column-major layouts correctly
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 operator *(Matrix4x4 a, Matrix4x4 b)
    {
        return new Matrix4x4(
            // Row 1
            a.M11 * b.M11 + a.M12 * b.M21 + a.M13 * b.M31 + a.M14 * b.M41,
            a.M11 * b.M12 + a.M12 * b.M22 + a.M13 * b.M32 + a.M14 * b.M42,
            a.M11 * b.M13 + a.M12 * b.M23 + a.M13 * b.M33 + a.M14 * b.M43,
            a.M11 * b.M14 + a.M12 * b.M24 + a.M13 * b.M34 + a.M14 * b.M44,

            // Row 2
            a.M21 * b.M11 + a.M22 * b.M21 + a.M23 * b.M31 + a.M24 * b.M41,
            a.M21 * b.M12 + a.M22 * b.M22 + a.M23 * b.M32 + a.M24 * b.M42,
            a.M21 * b.M13 + a.M22 * b.M23 + a.M23 * b.M33 + a.M24 * b.M43,
            a.M21 * b.M14 + a.M22 * b.M24 + a.M23 * b.M34 + a.M24 * b.M44,

            // Row 3
            a.M31 * b.M11 + a.M32 * b.M21 + a.M33 * b.M31 + a.M34 * b.M41,
            a.M31 * b.M12 + a.M32 * b.M22 + a.M33 * b.M32 + a.M34 * b.M42,
            a.M31 * b.M13 + a.M32 * b.M23 + a.M33 * b.M33 + a.M34 * b.M43,
            a.M31 * b.M14 + a.M32 * b.M24 + a.M33 * b.M34 + a.M34 * b.M44,

            // Row 4
            a.M41 * b.M11 + a.M42 * b.M21 + a.M43 * b.M31 + a.M44 * b.M41,
            a.M41 * b.M12 + a.M42 * b.M22 + a.M43 * b.M32 + a.M44 * b.M42,
            a.M41 * b.M13 + a.M42 * b.M23 + a.M43 * b.M33 + a.M44 * b.M43,
            a.M41 * b.M14 + a.M42 * b.M24 + a.M43 * b.M34 + a.M44 * b.M44
        );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector3 operator *(Matrix4x4 m, Vector3 v)
    {
        var x = m.M11 * v.X + m.M12 * v.Y + m.M13 * v.Z + m.M14;
        var y = m.M21 * v.X + m.M22 * v.Y + m.M23 * v.Z + m.M24;
        var z = m.M31 * v.X + m.M32 * v.Y + m.M33 * v.Z + m.M34;
        return new Vector3(x, y, z);
    }

    // --- Conversion for GL upload (column-major float[16], as GLSL expects) ---

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void WriteColumnMajor(Span<float> destination)
    {
        destination[0] = M11; destination[1] = M21; destination[2] = M31; destination[3] = M41;
        destination[4] = M12; destination[5] = M22; destination[6] = M32; destination[7] = M42;
        destination[8] = M13; destination[9] = M23; destination[10] = M33; destination[11] = M43;
        destination[12] = M14; destination[13] = M24; destination[14] = M34; destination[15] = M44;
    }

    public float[] ToColumnMajorArray()
    {
        var result = new float[16];
        WriteColumnMajor(result);
        return result;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Matrix4x4 CreateTranslation(Vector3 position)
    {
        return new Matrix4x4(
            1, 0, 0, position.X,
            0, 1, 0, position.Y,
            0, 0, 1, position.Z,
            0, 0, 0, 1
        );
    }

    // --- Equality & Boilerplate ---

    public bool Equals(Matrix4x4 other) =>
        M11.Equals(other.M11) && M12.Equals(other.M12) && M13.Equals(other.M13) && M14.Equals(other.M14) &&
        M21.Equals(other.M21) && M22.Equals(other.M22) && M23.Equals(other.M23) && M24.Equals(other.M24) &&
        M31.Equals(other.M31) && M32.Equals(other.M32) && M33.Equals(other.M33) && M34.Equals(other.M34) &&
        M41.Equals(other.M41) && M42.Equals(other.M42) && M43.Equals(other.M43) && M44.Equals(other.M44);

    public override bool Equals(object? obj) => obj is Matrix4x4 other && Equals(other);

    public override int GetHashCode()
    {
        var hc = new HashCode();
        hc.Add(M11); hc.Add(M12); hc.Add(M13); hc.Add(M14);
        hc.Add(M21); hc.Add(M22); hc.Add(M23); hc.Add(M24);
        hc.Add(M31); hc.Add(M32); hc.Add(M33); hc.Add(M34);
        hc.Add(M41); hc.Add(M42); hc.Add(M43); hc.Add(M44);
        return hc.ToHashCode();
    }

    public static bool operator ==(Matrix4x4 left, Matrix4x4 right) => left.Equals(right);
    public static bool operator !=(Matrix4x4 left, Matrix4x4 right) => !left.Equals(right);

    public override string ToString() =>
        $"[{M11}, {M12}, {M13}, {M14}]\n[{M21}, {M22}, {M23}, {M24}]\n[{M31}, {M32}, {M33}, {M34}]\n[{M41}, {M42}, {M43}, {M44}]";
}
