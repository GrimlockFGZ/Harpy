using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Engine;

/// <summary>
/// A high-performance TRS (Translation, Rotation, Scale) transform.
/// Immutable by default — all mutation returns a new Transform.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
public readonly struct Transform : IEquatable<Transform>
{
    public readonly Vector3 Position;
    public readonly Quaternion Rotation;
    public readonly Vector3 Scale;

    // --- Static Constants ---
    public static readonly Transform Identity = new(Vector3.Zero, Quaternion.Identity, Vector3.One);

    // --- Constructors ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Transform(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        Position = position;
        Rotation = rotation;
        Scale = scale;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Transform(Vector3 position) : this(position, Quaternion.Identity, Vector3.One) { }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Transform(Vector3 position, Quaternion rotation) : this(position, rotation, Vector3.One) { }

    // --- Derived Properties ---
    public Vector3 Forward => Rotation.Rotate(Vector3.Forward);
    public Vector3 Right => Rotation.Rotate(Vector3.Right);
    public Vector3 Up => Rotation.Rotate(Vector3.Up);

    public bool IsUniformScale => 
        MathF.Abs(Scale.X - Scale.Y) < 1e-6f && MathF.Abs(Scale.Y - Scale.Z) < 1e-6f;

    // --- Fluent Mutators ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Transform WithPosition(Vector3 position) => new(position, Rotation, Scale);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Transform WithRotation(Quaternion rotation) => new(Position, rotation, Scale);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Transform WithScale(Vector3 scale) => new(Position, Rotation, scale);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Transform WithScale(float uniformScale) => new(Position, Rotation, new Vector3(uniformScale));

    // --- Space Transformations ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 TransformPoint(Vector3 localPoint)
    {
        // TRS Order: Scale -> Rotate -> Translate
        Vector3 scaled = new(localPoint.X * Scale.X, localPoint.Y * Scale.Y, localPoint.Z * Scale.Z);
        return Rotation.Rotate(scaled) + Position;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 TransformDirection(Vector3 localDirection) => Rotation.Rotate(localDirection);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 InverseTransformPoint(Vector3 worldPoint)
    {
        Vector3 unTranslated = worldPoint - Position;
        Vector3 unRotated = Rotation.InverseRotate(unTranslated);
        
        // Guard against division by zero in scale
        return new Vector3(
            Scale.X != 0f ? unRotated.X / Scale.X : 0f,
            Scale.Y != 0f ? unRotated.Y / Scale.Y : 0f,
            Scale.Z != 0f ? unRotated.Z / Scale.Z : 0f);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Vector3 InverseTransformDirection(Vector3 worldDirection) => Rotation.InverseRotate(worldDirection);

    // --- Combination ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Transform Combine(Transform parent, Transform child)
    {
        Vector3 worldPos = parent.TransformPoint(child.Position);
        Quaternion worldRot = parent.Rotation * child.Rotation;
        Vector3 worldScale = new(
            parent.Scale.X * child.Scale.X,
            parent.Scale.Y * child.Scale.Y,
            parent.Scale.Z * child.Scale.Z);

        return new Transform(worldPos, worldRot, worldScale);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Transform Inverse()
    {
        Quaternion invRot = Quaternion.Conjugate(Rotation);
        Vector3 invScale = new(
            Scale.X != 0f ? 1f / Scale.X : 0f,
            Scale.Y != 0f ? 1f / Scale.Y : 0f,
            Scale.Z != 0f ? 1f / Scale.Z : 0f);

        // Position inversion: Rotate and scale the negative translation
        Vector3 invPos = invRot.Rotate(new Vector3(
            -Position.X * invScale.X,
            -Position.Y * invScale.Y,
            -Position.Z * invScale.Z));

        return new Transform(invPos, invRot, invScale);
    }

    // --- Interpolation ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Transform Lerp(Transform a, Transform b, float t)
    {
        return new Transform(
            Vector3.Lerp(a.Position, b.Position, t),
            Quaternion.Slerp(a.Rotation, b.Rotation, t),
            Vector3.Lerp(a.Scale, b.Scale, t)
        );
    }

    // --- Operators ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Transform operator *(Transform parent, Transform child) => Combine(parent, child);

    // --- Equality & Boilerplate ---
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Transform other) => 
        Position == other.Position && Rotation == other.Rotation && Scale == other.Scale;

    public override bool Equals(object? obj) => obj is Transform t && Equals(t);
    public override int GetHashCode() => HashCode.Combine(Position, Rotation, Scale);
    public static bool operator ==(Transform a, Transform b) => a.Equals(b);
    public static bool operator !=(Transform a, Transform b) => !a.Equals(b);
    
    

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out Vector3 position, out Quaternion rotation, out Vector3 scale)
    {
        position = Position;
        rotation = Rotation;
        scale = Scale;
    }

    public override string ToString() => $"Transform(P={Position}, R={Rotation}, S={Scale})";
}